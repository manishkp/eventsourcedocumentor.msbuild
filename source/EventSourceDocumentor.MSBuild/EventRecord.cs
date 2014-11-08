// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventRecord.cs">
//   Copyright belongs to Manish Kumar
// </copyright>
// <summary>
//   Build task to return generate documentation for events value
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace EventSourceDocumentor.MSBuild
{
    /// <summary>
    /// The event record.
    /// </summary>
    public class EventRecord
    {
        /// <summary>
        /// Gets or sets the event name.
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets the event id.
        /// </summary>
        public string EventId { get; set; }

        /// <summary>
        /// Gets or sets the event level.
        /// </summary>
        public string EventLevel { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the resolution.
        /// </summary>
        public string Resolution { get; set; }
    }
}
