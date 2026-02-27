using System;
using Rhino;
using Rhino.Commands;

namespace SecondPlugin
{
    public class ReactiveTestCommand : Command
    {
        public static void ReactiveUndoEventHandler(object sender, CustomUndoEventArgs e)
        {
            RhinoApp.WriteLine("ReactiveTestCommand Custom Undo Event Triggered. Tag: {0}", e.ActionDescription);
            e.Document.AddCustomUndoEvent("Redo Reactive Test Command", ReactiveRedoEventHandler, e.CommandId);
        }

        public static void ReactiveRedoEventHandler(object sender, CustomUndoEventArgs e)
        {
            RhinoApp.WriteLine("ReactiveTestCommand Custom Redo Event Triggered. Tag: {0}", e.ActionDescription);
            e.Document.AddCustomUndoEvent("Undo Reactive Test Command", ReactiveUndoEventHandler, e.CommandId);
        }

        public ReactiveTestCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static ReactiveTestCommand Instance { get; private set; }

        public override string EnglishName => "ReactiveTestCommand";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("Running ReactiveTestCommand. Adding custom undo event.");
            doc.AddCustomUndoEvent("Undo Reactive Test Command", ReactiveUndoEventHandler, Guid.NewGuid());

            // TODO: complete command.
            return Result.Success;
        }
    }
}