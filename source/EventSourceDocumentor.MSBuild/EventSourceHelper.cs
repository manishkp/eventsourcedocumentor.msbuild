// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventSourceHandler.cs">
//   Copyright belongs to Manish Kumar
// </copyright>
// <summary>
//   Build task to return generate documentation for events value
// </summary>
// --------------------------------------------------------------------------------------------------------------------


namespace EventSourceDocumentor.MSBuild
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Xml.Linq;
    using System.Xml.XPath;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// EventSource Documentation Helper 
    /// </summary>
    public class EventSourceHelper
    {
        /// <summary>
        /// The get event source class.
        /// </summary>
        /// <param name="filePath">
        /// The file path.
        /// </param>
        /// <returns>
        /// The <see cref="ClassDeclarationSyntax"/>.
        /// </returns>
        public static ClassDeclarationSyntax GetEventSourceClass(string filePath)
        {
            var classText = File.ReadAllText(filePath);
            var classTree = CSharpSyntaxTree.ParseText(classText);
            var classRoot = (CompilationUnitSyntax)classTree.GetRoot();
            var ns = classRoot.Members.SingleOrDefault(m => m is NamespaceDeclarationSyntax) as NamespaceDeclarationSyntax;
           
            if (ns == null)
            {
                // Assemblyinfo.cs doesnt have namespace
                return null;
            }

            // looking for EventSource as the base type of the class
            var eventSourceClass =
                ns.Members.SingleOrDefault(
                    m =>
                    (m is ClassDeclarationSyntax
                    && (m as ClassDeclarationSyntax).BaseList != null
                     && ((m as ClassDeclarationSyntax).BaseList.Types.SingleOrDefault(
                         t => (t.ToString() == "EventSource")) != null))) as ClassDeclarationSyntax;

            return eventSourceClass;
        }

        /// <summary>
        /// The get event source name.
        /// </summary>
        /// <param name="eventSourceClass">
        /// The event source class.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Event Source class is empty
        /// </exception>
        public static string GetEventSourceName(ClassDeclarationSyntax eventSourceClass)
        {
            if (eventSourceClass == null)
            {
                // this class is not a EventSource class
                throw new ArgumentNullException("eventSourceClass", "eventSourceClass cannot be null");
            }

            // looking for eventSource attribute on the class
            // [EventSource(Name = EventSourceName)]
            var eventSourceAttribute =
                eventSourceClass.AttributeLists.Select(
                    attrs => attrs.Attributes.SingleOrDefault(a => a.Name.ToString() == "EventSource"))
                    .SingleOrDefault(attr => attr != null);

            var eventSourceNameExpression =
                eventSourceAttribute.ArgumentList.Arguments.SingleOrDefault(
                    arg => arg.NameEquals.Name.Identifier.Text == "Name");
            return EvaluateExpression(eventSourceClass, eventSourceNameExpression.Expression);
        }

        /// <summary>
        /// The process event source class.
        /// </summary>
        /// <param name="eventSourceClass">
        /// The event Source Class.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>. of records
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Event Source class is empty
        /// </exception>
        public static IEnumerable<EventRecord> GetAllEventRecords(ClassDeclarationSyntax eventSourceClass)
        {
            if (eventSourceClass == null)
            {
                // this class is not a EventSource class
                throw new ArgumentNullException("eventSourceClass", "eventSourceClass cannot be null");
            }

            // Find all the Event Methods, which have Event Attribute
            // [Event(
            //  EventIds.UnhandledException,
            //  Message = "Unhandled exception caught while processing '{0}.{1}'. {2} Error:{3}",
            //  Level = EventLevel.Informational,
            //  Task = Tasks.GenericMethodCall,
            //  Opcode = Opcodes.UnhandledException,
            //  Channel = EventChannel.Operational,
            //  Version = 0x03
            //  )]
            var methods =
                eventSourceClass.Members.OfType<MethodDeclarationSyntax>()
                    .Where(
                        mtd =>
                        mtd.AttributeLists.Where(
                            attr => attr.Attributes.Where(a => a.Name.ToString() == "Event").ToList().Count != 0)
                            .ToList()
                            .Count != 0)
                    .ToList();

            return methods.Select(
                method =>
                    {
                        // get details out of Event attribute of the event method
                        var eventAttribute =
                            method.AttributeLists.Select(
                                attr => attr.Attributes.SingleOrDefault(a => a.Name.ToString() == "Event"))
                                .SingleOrDefault(attr => attr != null);

                        var eventIdExpression =
                            eventAttribute.ArgumentList.Arguments.SingleOrDefault(
                                arg => arg.NameEquals == null || arg.NameEquals.Name.Identifier.Text == "Id");

                        var eventId = string.Empty;

                        if (eventIdExpression != null)
                        {
                            eventId = EvaluateExpression(eventSourceClass, eventIdExpression.Expression);
                        }

                        var eventLevelExpression =
                            eventAttribute.ArgumentList.Arguments.SingleOrDefault(
                                arg => arg.NameEquals != null && arg.NameEquals.Name.Identifier.Text == "Level");
                        var eventLevel = "Informational";
                        if (eventLevelExpression != null)
                        {
                            eventLevel = EvaluateExpression(eventSourceClass, eventLevelExpression.Expression);
                        }

                        // get details out of comments on the event attribute
                        SyntaxTriviaList trivia = method.GetLeadingTrivia();
                        var summary = string.Empty;
                        var resolution = string.Empty;

                        SyntaxTrivia comment =
                            trivia.FirstOrDefault(
                                t => (SyntaxKind)t.RawKind == SyntaxKind.SingleLineDocumentationCommentTrivia);

                        if (comment != null)
                        {
                            var commentsNode = XDocument.Parse("<comments>" + comment.ToString() + "</comments>");
                            summary = FormatLinesInCommentSection(commentsNode, "summary");
                            resolution = FormatLinesInCommentSection(commentsNode, "resolution");
                        }

                        return new EventRecord()
                                   {
                                       EventName = method.Identifier.Text,
                                       Description = summary,
                                       EventId = eventId,
                                       EventLevel = eventLevel,
                                       Resolution = resolution
                                   };
                    });
        }

        /// <summary>
        /// Formats the section from comments for preserving new line and still removing starting /// and space
        /// </summary>
        /// <param name="commentsXml">Comments XML</param>
        /// <param name="commentSectionName">Name of the section in comment</param>
        /// <returns>Formatter comments section</returns>
        private static string FormatLinesInCommentSection(XDocument commentsXml, string commentSectionName)
        {
            var lines =
                (commentsXml.XPathSelectElement("comments/" + commentSectionName) ?? new XElement("dummy")).Value.Split(
                    Environment.NewLine.ToCharArray(),
                    StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.TrimStart().Replace(@"///", string.Empty));
            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// The evaluate expression.
        /// </summary>
        /// <param name="parentClass">
        /// The parent class.
        /// </param>
        /// <param name="expression">
        /// The expression.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string EvaluateExpression(ClassDeclarationSyntax parentClass, ExpressionSyntax expression)
        {
            string value = string.Empty;
            if ((SyntaxKind)expression.RawKind == SyntaxKind.SimpleMemberAccessExpression)
            {
                // we only support 2 level expressions
                var eventIdWithClass = expression.ToString();
                var eventIdParts = eventIdWithClass.Split('.');
                var eventIdClass = parentClass;
                var eventIdIdentifier = eventIdWithClass;
                if (eventIdParts.Length > 1)
                {
                    eventIdClass =
                        parentClass.Members.OfType<ClassDeclarationSyntax>()
                            .SingleOrDefault(
                                cls => ((ClassDeclarationSyntax)cls).Identifier.Text == eventIdParts[0]);

                    eventIdIdentifier = eventIdParts[1];
                }

                if (eventIdClass != null)
                {
                    var expressionFieldVar =
                        eventIdClass.Members.OfType<FieldDeclarationSyntax>()
                            .Select(
                                fld =>
                                fld.Declaration.Variables.SingleOrDefault(
                                    var => var.Identifier.Text == eventIdIdentifier))
                            .SingleOrDefault(var => var != null);
                    value = EvaluateExpression(eventIdClass, expressionFieldVar.Initializer.Value);
                }
                else
                {
                    // this is case for types defined outside this class like EventLevel,EventChannel
                    value = eventIdIdentifier;
                }
            }
            else if (((SyntaxKind)expression.RawKind == SyntaxKind.IdentifierToken) || ((SyntaxKind)expression.RawKind == SyntaxKind.IdentifierName))
            {
                var eventIdIdentifier = expression.ToString();

                var expressionFieldVar =
                   parentClass.Members.OfType<FieldDeclarationSyntax>()
                       .Select(
                           fld =>
                           fld.Declaration.Variables.SingleOrDefault(
                               var => var.Identifier.Text == eventIdIdentifier))
                       .SingleOrDefault(var => var != null);
                value = EvaluateExpression(parentClass, expressionFieldVar.Initializer.Value);
            }
            else if ((SyntaxKind)expression.RawKind == SyntaxKind.NumericLiteralExpression)
            {
                value = ((LiteralExpressionSyntax)expression).Token.ValueText;
            }
            else if ((SyntaxKind)expression.RawKind == SyntaxKind.StringLiteralExpression)
            {
                value = ((LiteralExpressionSyntax)expression).Token.ValueText;
            }

            return value;
        }
    }
}
