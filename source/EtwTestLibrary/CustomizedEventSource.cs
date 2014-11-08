namespace EtwTestLibrary
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Diagnostics.Tracing;

    [EventSource(Name = "Samples-EventSourceDemos-Customized")]
    public sealed class CustomizedEventSource : EventSource
    {
        #region Singleton instance
        static public CustomizedEventSource Log = new CustomizedEventSource();
        #endregion

        [Event(1, Keywords = Keywords.Requests,
               Task = Tasks.Request, Opcode = EventOpcode.Start)]
        public void RequestStart(int RequestID, string Url)
        { WriteEvent(1, RequestID, Url); }

        [Event(2, Keywords = Keywords.Requests, Level = EventLevel.Verbose,
               Task = Tasks.Request, Opcode = EventOpcode.Info)]
        public void RequestPhase(int RequestID, string PhaseName)
        { WriteEvent(2, RequestID, PhaseName); }

        [Event(3, Keywords = Keywords.Requests,
               Task = Tasks.Request, Opcode = EventOpcode.Stop)]
        public void RequestStop(int RequestID)
        { WriteEvent(3, RequestID); }

        [Event(4, Keywords = Keywords.Debug)]
        public void DebugTrace(string Message)
        { WriteEvent(4, Message); }

        #region Keywords / Tasks / Opcodes

        public class Keywords   // This is a bitvector
        {
            public const EventKeywords Requests = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
        }

        public class Tasks
        {
            public const EventTask Request = (EventTask)0x1;
        }

        #endregion
    }
}
