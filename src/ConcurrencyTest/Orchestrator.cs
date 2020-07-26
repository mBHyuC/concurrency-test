using Concurrency.Model;

using System.Threading;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

using System.Threading.Tasks;
using System.Collections.Generic;

using System.Runtime.CompilerServices;

namespace Concurrency
{
    public static class Helper
    {
        public static (IEnumerable<T> matches, IEnumerable<T> nonMatches) Fork<T>(
            this IEnumerable<T> source,
            Func<T, bool> pred)
        {
            var groupedByMatching = source.ToLookup(pred);
            return (groupedByMatching[true], groupedByMatching[false]);
        }

        public static IEnumerable<T[]> AsChunks<T>(this IEnumerable<T> source, int chunkMaxSize)
        {
            var arr = new T[chunkMaxSize];
            var pos = 0;
            foreach (var item in source)
            {
                arr[pos++] = item;
                if (pos == chunkMaxSize)
                {
                    yield return arr;
                    arr = new T[chunkMaxSize];
                    pos = 0;
                }
            }
            if (pos > 0)
            {
                Array.Resize(ref arr, pos);
                yield return arr;
            }
        }

        public static void Iter<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
        {
            foreach (var item in source)
                action(item);
        }
    }

    class Executer
    {
        private readonly string Guid;
        private PersistentStorage DbHelper;
        private Generator Generator;
        private IndicatorProcessor Processor;

        public Executer(string guid, string connectionString)
        {
            Guid = guid;

            DbHelper = new PersistentStorage(guid, connectionString);
            Generator = new Generator(guid, DbHelper, 1000000, 1000);
            Processor = new IndicatorProcessor(guid, DbHelper, 1000);
        }

        public async Task StartAsync(CancellationToken token)
        {
            OrchestratorEvents.Log.Message("Executer StartAsync");

            int currentRunId = -1;
            int retryCounterMax = 3000;
            int retryCounter = retryCounterMax;
            Task genTask;
            Task processTask;

            try
            {

                using (DataContext context = DbHelper.GetNewContext())
                {
                    OrchestratorEvents.Log.Message("Creating new Run Object");
                    var currentRun = new Concurrency.Model.Run();
                    currentRun.Start = DateTimeOffset.Now;
                    currentRun.Guid = Guid;
                    currentRun.Name = Guid;
                    context.Runs.Add(currentRun);

                    await context.SaveChangesAsync();

                    currentRunId = currentRun.Id;

                    OrchestratorEvents.Log.Message($"Added Run with ID {currentRunId} and Config");


                    await DbHelper.CleanUpWorklist(token);

                    OrchestratorEvents.Log.Message("Cleaned Up Workerlist");


                    genTask = Generator.StartAsync(token, currentRunId);
                    currentRun.Status = RunState.GeneratorRunningTask;
                    // trigger status report
                    await context.SaveChangesAsync();
                    OrchestratorEvents.Log.Message("Started Generator");

                    //start the processor as well but do set the RunState to GeneratorRunningTask once the generator has fully completeted
                    processTask = Processor.StartAsync(token, currentRunId);

                    OrchestratorEvents.Log.Message("Started Processor");
                }
            }
            catch (Exception ex)
            {
                OrchestratorEvents.Log.DebugMessage(ex.Message);
                OrchestratorEvents.Log.DebugMessage("Error exit executor stage 0. All results of this run are invalid");
                return;
            }


            //break condition
            if (currentRunId < 0)
            {
                OrchestratorEvents.Log.DebugMessage("Error exit executor stage 1. All results of this run are invalid");
                return;
            }

            while (!genTask.Wait(2000)) // do not cancel the task!
            {
                if (Processor.IsPaused)
                {
                    // trigger status report
                }
                else
                {
                    // trigger status report
                }
            }

            while (retryCounter > 0)
            {
                try
                {
                    using (DataContext context = DbHelper.GetNewContext())
                    {
                        var currentRun = context.Runs.Where(c => c.Id == currentRunId).First();
                        currentRun.Status = RunState.ProcessorRunningTask;
                        await context.SaveChangesAsync();

                        OrchestratorEvents.Log.Message("Finisched Generator");
                    }
                    break;
                }
                catch (Exception ex)
                {
                    retryCounter -= 1;
                    OrchestratorEvents.Log.DebugMessage(ex.ToString());
                    OrchestratorEvents.Log.DebugMessage($"Current retry counter value {retryCounter}");
                    await Task.Delay(5000);

                }
            }

            //break condition
            if (retryCounter != 0)
            {
                retryCounter = retryCounterMax;
            }
            else
            {
                OrchestratorEvents.Log.DebugMessage("Error exit executor stage 2. All results of this run are invalid");
                return;
            }


            while (!processTask.Wait(2000)) // do not cancel the task!
            {
                if (Processor.IsPaused)
                {
                    // trigger status report
                    await Task.Delay(5000);
                }
                else
                {
                    // trigger status report
                }
            }

            while (retryCounter > 0)
            {
                try
                {
                    using (DataContext context = DbHelper.GetNewContext())
                    {
                        // Not sure about the changes already did to the database
                        if (token.IsCancellationRequested)
                        {
                            // trigger status report
                        }
                        else
                        {
                            var currentRun = context.Runs.Where(c => c.Id == currentRunId).First();
                            currentRun.End = DateTimeOffset.Now;
                            currentRun.Status = RunState.DoneTask;

                            await context.SaveChangesAsync();

                            // trigger status report
                        }
                        OrchestratorEvents.Log.Message("Finisched Processor");
                    }
                    break;
                }
                catch (Exception ex)
                {
                    retryCounter -= 1;
                    OrchestratorEvents.Log.DebugMessage(ex.ToString());
                    OrchestratorEvents.Log.DebugMessage($"Current retry counter value {retryCounter}");
                    await Task.Delay(5000);

                }
            }

            //break condition
            if (retryCounter == 0)
            {
                OrchestratorEvents.Log.DebugMessage("Error exit executor stage 3. All results of this run are invalid");
                return;
            }
        }

    }

    class PersistentStorage
    {
        private string DbParameter;
        private Func<string, DataContext> DbContextCreator;
        private readonly string Guid;

        public PersistentStorage(string guid, string connectionString)
        {
            Guid = guid;

            DbContextCreator = DataContext.CreateSqlServerContext;
            DbParameter = connectionString;
        }

        public DataContext GetNewContext()
        {
            return DbContextCreator(DbParameter);
        }

        public async Task<int> CleanUpWorklist(CancellationToken token)
        {
            try
            {
                using (var context = GetNewContext())
                {
                    return await context.Database.ExecuteSqlRawAsync("DELETE FROM WorkItems", token);
                    //context.WorkItems.RemoveRange(context.WorkItems);
                    //return await context.SaveChangesAsync(token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                OrchestratorEvents.Log.DebugMessage(ex.ToString());
                return -1;
            }
        }

        public async void SaveBatchedWorkload(string[] workdata)
        {
            try
            {
                using (var context = GetNewContext())
                {
                    try
                    {

                        var workitems = workdata.Select(c => new WorkItem()
                        {
                            Name = c
                        });

                        await context.WorkItems.AddRangeAsync(workitems);
                        await context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        OrchestratorEvents.Log.DebugMessage(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                OrchestratorEvents.Log.DebugMessage(ex.ToString());
            }
        }

        public async IAsyncEnumerable<List<WorkItem>> LoadBatchedWorkload([EnumeratorCancellation] CancellationToken token, int batchSize, int runID)
        {
            DataContext context = null;
            try
            {
                context = GetNewContext();
                List<WorkItem> workItems;
                bool loadSuccess;
                bool loadSuccessInner;
                while (true)
                {
                    if (token.IsCancellationRequested) break;

                    loadSuccess = false;
                    loadSuccessInner = false;

                    //alwasy dispose and recreate!
                    // will rise the chance of success if multiple processors try to get data
                    context.Dispose();
                    context = GetNewContext();

                    RunState currentRunState;

                    try
                    {
                        currentRunState = context.Runs.Where(a => a.Id == runID).First().Status;
                        //if(true)
                        //{
                        //    throw Microsoft.Data.SqlClient.SqlException;
                        //}
                        workItems = context.WorkItems.Where(a => a.State == ProcessState.ToProcess).Take(batchSize).ToList();
                        loadSuccessInner = true;
                    }
                    catch (Exception ex)
                    {
                        OrchestratorEvents.Log.DebugMessage(ex.ToString());
                        currentRunState = RunState.GeneratorRunningTask;
                        workItems = new List<WorkItem>();
                    }

                    //force a valid result of the database
                    if (workItems.Count == 0 && currentRunState == RunState.ProcessorRunningTask && loadSuccessInner)
                    {
                        break;
                    }
                    else if (workItems.Count == 0)
                    {
                        await Task.Delay(5000, token).ConfigureAwait(false);
                    }
                    else
                    {
                        // Try to set the InProcessFlag for these item, if success 
                        // → exclusivly locked and ready to process
                        // → on failure (another process was faster and did take part of these items) dispose everything and try to get new items
                        foreach (var workItem in workItems)
                        {
                            workItem.State = ProcessState.InProcess;
                        }

                        try
                        {
                            await context.SaveChangesAsync(token).ConfigureAwait(false);
                            loadSuccess = true;
                        }
                        catch (DbUpdateConcurrencyException ex)
                        {
                            //reset the contenxt
                            context.Dispose();
                            context = GetNewContext();
                            OrchestratorEvents.Log.Message("Failed load entries1");
                        }
                        catch (DbUpdateException ex)
                        {
                            OrchestratorEvents.Log.Message("Failed load entries2");
                        }
                        if (loadSuccess) yield return workItems;
                        if (workItems.Count < batchSize && currentRunState == RunState.ProcessorRunningTask)
                        {
                            break;
                        }
                    }
                }
            }
            //catch (Exception ex)
            //{
            //    var tt = ex.Message;
            //}
            finally
            {
                if (context != null) context.Dispose();
            }
        }
    }

    class Generator
    {
        private readonly int ChunkSize;
        private readonly int SampleSize;
        private PersistentStorage Storage;
        public int Generated;
        private readonly string Guid;
        private Task t = null;

        public Generator(string guid, PersistentStorage storage, int sampleSize, int chunkSize)
        {
            Guid = guid;

            this.Storage = storage;
            this.ChunkSize = chunkSize;
            this.SampleSize = sampleSize;
            Generated = 0;


        }

        public Task StartAsync(CancellationToken token, int runId)
        {
            if (t == null || !((t.Status > TaskStatus.Created) && (t.Status < TaskStatus.RanToCompletion)))
            {
                t = new Task(() =>
                {
                    for (var i = 0; i < SampleSize; i += ChunkSize)
                    {
                        Thread.Sleep(500);
                        var d = Enumerable.Range(0, ChunkSize).Select(c => System.Guid.NewGuid().ToString()).ToArray();
                        Generated += d.Length;
                        Storage.SaveBatchedWorkload(d);
                    }
                });
                t.Start();
            }

            return t;
        }
    }

    class IndicatorProcessor
    {
        private PersistentStorage Storage;
        private Task Tsk;
        private int ChunkSize;
        private readonly string Guid;
        public int Processed;

        public bool IsPaused = false;


        public IndicatorProcessor(string guid, PersistentStorage storage, int chunkSize)
        {
            Guid = guid;

            Storage = storage;
            ChunkSize = chunkSize;
            

            Processed = 0;

        }

        public Task StartAsync(CancellationToken token, int runId)
        {
            Func<Task<int>> Processor = async () =>
            {
                DataContext context = null; //direkt 
                if (token.IsCancellationRequested) return 0;
                try
                {
                    context = Storage.GetNewContext();
                    //context.Database.L = s => System.Diagnostics.Debug.WriteLine(s);
                    //using (var transaction = context.Database.BeginTransaction())
                    //{
                    try
                    {

                        int retryCounter = 30;
                        OrchestratorEvents.Log.Message($"Setting failure recovery retry counter to {retryCounter}.");

                        await foreach (List<WorkItem> worklist in Storage.LoadBatchedWorkload(token, ChunkSize, runId))
                        {
                            while (retryCounter > 0)
                            {
                                try
                                {
                                    context.Dispose();
                                    context = Storage.GetNewContext();

                                    // to crazy amount of work here and then set the state to processed
                                    // redo everything on failure  
                                    await Task.Delay(1000);

                                    try
                                    {
                                        //when processes fine
                                        foreach (var workitem in worklist)
                                        {
                                            workitem.State = ProcessState.DoneProcessOK;
                                            context.WorkItems.Update(workitem);
                                        }
                                        await context.SaveChangesAsync();
                                        Processed += worklist.Count;
                                        OrchestratorEvents.Log.Message($"Processed {Processed} items.");
                                    }
                                    catch (Exception ex)
                                    {
                                        OrchestratorEvents.Log.DebugMessage(ex.ToString());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    retryCounter -= 1;
                                    OrchestratorEvents.Log.DebugMessage(ex.ToString());
                                    OrchestratorEvents.Log.DebugMessage("Retries outer left " + retryCounter.ToString());
                                    await Task.Delay(5000);
                                }

                            }
                            if (retryCounter == 0)
                            {
                                OrchestratorEvents.Log.DebugMessage("Forcing applicaiton to end. The result is not vaild.");
                                break;
                            }
                        }
                        //        if (token.IsCancellationRequested) await transaction.RollbackAsync().ConfigureAwait(false);
                        //        else await transaction.CommitAsync().ConfigureAwait(false);
                    } //END TRANSACTION TRY CATCH
                    catch (Exception ex)
                    {
                        OrchestratorEvents.Log.DebugMessage(ex.ToString());
                        //        await transaction.RollbackAsync().ConfigureAwait(false);
                    }
                    //} //END TRANSACTION 
                } //END TASK TRY CATCH
                catch (Exception ex)
                {
                    OrchestratorEvents.Log.DebugMessage(ex.ToString());
                }
                finally
                {
                    if (context != null) context.Dispose();
                }
                return 0;

            };

            Tsk = Task.Run(async () =>
            {
                var a = await Processor();
            });
            //Tsk.Start();
            return Tsk;
        }
    }


}