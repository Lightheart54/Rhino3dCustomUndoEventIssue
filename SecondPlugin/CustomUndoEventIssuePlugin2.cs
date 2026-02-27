using Rhino;
using Rhino.Commands;
using Rhino.PlugIns;
using SecondPlugin;
using System;
using System.Collections.Generic;

namespace CustomUndoEventIssue;
///<summary>
/// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
/// class. DO NOT create instances of this class yourself. It is the
/// responsibility of Rhino to create an instance of this class.</para>
/// <para>To complete plug-in information, please also see all PlugInDescription
/// attributes in AssemblyInfo.cs (you might need to click "Project" ->
/// "Show All Files" to see it in the "Solution Explorer" window).</para>
///</summary>
public class CustomUndoEventIssuePlugin2 : Rhino.PlugIns.PlugIn
{
    public CustomUndoEventIssuePlugin2()
    {
        Instance = this;
    }

    ///<summary>Gets the only instance of the CustomUndoEventIssuePlugin2 plug-in.</summary>
    public static CustomUndoEventIssuePlugin2 Instance { get; private set; }

    // You can override methods here to change the plug-in behavior on
    // loading and shut down, add options pages to the Rhino _Option command
    // and maintain plug-in wide options in a document.

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        RhinoApp.WriteLine("CustomUndoEventIssuePlugin2 loaded.");

        //What works is to set up a completely separate undo/redo stack and execute commands in response to the undo/redo events,
        //My gut tells the that this feels wrong. Like with setting user dictionaries and user test on the document or layers,
        //We'll have to artificially push an event onto the rhino undo stack if the command or action wouldn't otherwise create an undo event in rhino.
        //So when a developer is working in a command, the have to worry about whether the actions they are taking in their command will trigger undo/redo events,
        //and if not, they have to push dummy events onto the rhino undo stack so that their undo/redo events will be triggered.
        //This seems bad, but I don't see any other way to have reactive undo/redo events with the current rhino api.
        Command.UndoRedo += SeparateUndoRedoExecution;

        //two options that feel more ergonomic but don't work:
        // - run a command in response to the undo/redo event
        // - register a custom undo event in response to every non-undo/redo command.
        //Both have the same problem, they potentially register multiple undo events for a single command,
        //When this happens, rhino will trigger whichever event was registered first as many times as undo events were registered.
        //So if another plugin registers an undo event for a command,and then we append another undo event for the same command, then undoing
        //that command will trigger the plugin's undo event twice, which could cause all sorts of problems depending on what the undo event does.

        //Command.UndoRedo += ReactiveCommandExecution;
        //Command.EndCommand += ReactiveHandleRegistration;
        return LoadReturnCode.Success;
    }

    public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;


    private Stack<Guid> _undoStack = new Stack<Guid>();
    private Stack<Guid> _redoStack = new Stack<Guid>();

    private Guid _recordingCommandId = Guid.Empty;
    private void SeparateUndoRedoExecution(object sender, Rhino.Commands.UndoRedoEventArgs e)
    {
        if (e.IsBeforeBeginRecording)
        {
            RhinoApp.WriteLine($"Preparing to record a new command. Command Id: {e.CommandId.ToString()[..8]}");
            _recordingCommandId = e.CommandId;
            return;
        }
        if(e.IsBeginRecording) {
            RhinoApp.WriteLine($"Started recording a new command. Command Id: {e.CommandId.ToString()[..8]}");
            return; // we've already prepared for a new command in the BeforeBeginRecording event, no need to do anything in the BeginRecording event.
        }
        if (e.IsBeginUndo || e.IsBeginRedo)
        {
            RhinoApp.WriteLine($"Starting to {(e.IsBeginUndo ? "Undo" : "Redo")} Command. Command Id: {e.CommandId.ToString()[..8]}");
            return;
        }
        else if(e.IsEndUndo) 
        { 
            var undoCommandId = _undoStack.Pop();
            RhinoApp.WriteLine($"Undo Command Id: {undoCommandId.ToString()[..8]}");
            _redoStack.Push(undoCommandId);
        }
        else if(e.IsEndRedo)
        { 
            var redoCommandId = _redoStack.Pop();
            RhinoApp.WriteLine($"Redo Command Id: {redoCommandId.ToString()[..8]}");
            _undoStack.Push(redoCommandId);
        }


        if(e.IsBeforeEndRecording && e.CommandId != _recordingCommandId)
        {
            RhinoApp.WriteLine($"Ending Recording of Undo/Redo Command. Command Id: {e.CommandId.ToString()[..8]}");
        }
        else if(e.IsBeforeEndRecording)
        {
            RhinoApp.WriteLine($"New Command Recorded. Command Id: {e.CommandId.ToString()[..8]}");
            _undoStack.Push(e.CommandId);
        }
    }

    private Guid currentCommandId = Guid.Empty;

    private void ReactiveCommandExecution(object sender, Rhino.Commands.UndoRedoEventArgs e)
    {
        if (e.IsBeforeBeginRecording)
        {
            _recordingCommandId = e.CommandId;
            return;
        }
        else if (!e.IsBeforeEndRecording || e.CommandId != _recordingCommandId) return;
        //get the undo/redo commands ids


        if (e.CommandId != ReactiveTestCommand.Instance.Id)
        {
            var result = RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, ReactiveTestCommand.Instance.EnglishName);
            if (result != Rhino.Commands.Result.Success)
            {
                RhinoApp.WriteLine("Failed To Run ReactiveTestCommand");
            }
        }
    }

    private static void ReactiveUndoHandle(object sender, CustomUndoEventArgs e)
    {
        RhinoApp.WriteLine("Reactive Undo Event Triggered. Tag: {0}", e.ActionDescription);
        e.Document.AddCustomUndoEvent("Reactive Redo Event", ReactiveRedoHandle, e.CommandId);
    }

    private static void ReactiveRedoHandle(object sender, CustomUndoEventArgs e)
    {
        RhinoApp.WriteLine("Reactive Redo Event Triggered. Tag: {0}", e.ActionDescription);
        e.Document.AddCustomUndoEvent("Reactive Undo Event", ReactiveUndoHandle, e.CommandId);
    }

    private void ReactiveHandleRegistration(object sender, CommandEventArgs e)
    {
        if(e.CommandEnglishName.Contains("Undo") || e.CommandEnglishName.Contains("Redo")) return;
        var result = e.Document.AddCustomUndoEvent("Example reactive custom undo event", ReactiveUndoHandle, e.CommandId);
        if (result == false)
        {
            RhinoApp.WriteLine("Failed To Register Undo Event");
        }
    }

}