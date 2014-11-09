// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventSourceDocumentor.cs">
//   Copyright belongs to Manish Kumar
// </copyright>
// <summary>
//   Build task to return generate documentation for events value
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace EventSourceDocumentor.MSBuild
{
    using System;
    using System.IO;

    using CsvHelper;

    using Microsoft.Build.Framework;


    /// <summary>
    /// Build task to generate documentation for events
    /// </summary>
    public class EventSourceDocumentor : ITask
    {
        /// <summary>
        /// Gets or sets Build Engine
        /// </summary>
        public IBuildEngine BuildEngine { get; set; }

        /// <summary>
        /// Gets or sets Host Object
        /// </summary>
        public ITaskHost HostObject { get; set; }

        /// <summary>
        /// Gets or sets class Full Path
        /// </summary>
        [Required]
        public ITaskItem[] Sources { get; set; }

        /// <summary>
        /// Gets or sets output Full Path
        /// </summary>
        [Required]
        public ITaskItem OutputPath { get; set; }

        /// <summary>
        /// Gets or sets project Full Path
        /// </summary>
        [Required]
        public ITaskItem ProjectPath { get; set; }

        /// <summary>
        /// Executes the Task
        /// </summary>
        /// <returns>True if success</returns>
        public bool Execute()
        {
            // var projectpath = System.Environment.GetEnvironmentVariable("MSBuildProjectDirectory");
            foreach (var source in this.Sources)
            {
                var filePath = Path.Combine(this.ProjectPath.ItemSpec, source.ItemSpec);
                if (!File.Exists(filePath))
                {
                    this.BuildEngine.LogMessageEvent(
                        new BuildMessageEventArgs(
                            string.Format(
                                "Skipping EventSource document generation, as there are no files found at: {0}",
                                filePath),
                            string.Empty,
                            "EventSourceDocumentor",
                            MessageImportance.Normal));

                    continue;
                }

                this.BuildEngine.LogMessageEvent(
                       new BuildMessageEventArgs(
                           string.Format(
                               "Processing file: {0}",
                               filePath),
                           string.Empty,
                           "EventSourceDocumentor",
                           MessageImportance.Normal));

                var eventSourceClass = EventSourceHelper.GetEventSourceClass(filePath);

                if (eventSourceClass == null)
                {
                    this.BuildEngine.LogMessageEvent(
                       new BuildMessageEventArgs(
                           string.Format(
                               "Skipping Non EventSource class at: {0}",
                               source.ItemSpec),
                           string.Empty,
                           "EventSourceDocumentor",
                           MessageImportance.Normal));
                    continue;
                }

                var eventSourceName = EventSourceHelper.GetEventSourceName(eventSourceClass);
                
                this.BuildEngine.LogMessageEvent(
                      new BuildMessageEventArgs(
                          string.Format(
                              "Generating EventSource documentation for EventSource: {0}",
                              eventSourceName),
                          string.Empty,
                          "EventSourceDocumentor",
                          MessageImportance.Normal));

                var records = EventSourceHelper.GetAllEventRecords(eventSourceClass);

                var outputPath = Path.Combine(this.OutputPath.ItemSpec, eventSourceName + ".csv");
                using (StreamWriter writer = File.CreateText(outputPath))
                {
                    var csv = new CsvWriter(writer);
                    csv.WriteRecords(records);
                }

                this.BuildEngine.LogMessageEvent(
                      new BuildMessageEventArgs(
                          string.Format(
                              "EventSource documentation generated at: {0}",
                              outputPath),
                          string.Empty,
                          "EventSourceDocumentor",
                          MessageImportance.Normal));
            }

            return true;
        }
    }
}