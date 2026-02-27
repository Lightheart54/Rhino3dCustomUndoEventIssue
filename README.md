# CustomUndoEventIssue

This project demonstrates a potential issue with custom undo events in a RhinoCommon plugin.
The issue arises when multiple custom undo events are registered during a single recorded command, which can lead to unexpected behavior when undoing actions in Rhino.

The repo presents two ways of doing it wrong, and one way of doing it right. 
The wrong ways involve reactively registering additional custom undo events in response to the execution of a command. In one case we directly register that undo event and in another we reactively execute a second command which does so.

The right way involves producing a parrallel undo/redo stack that is independent of the one Rhino uses, and then synchronizing it with the Rhino undo/redo stack. 

Additionally I've included a simple script runner command that demonstrates that this issue is not limited to custom undo events registered reactively, but also occurs when a script runner command calls two commands that each register a custom undo event.

## Steps to Reproduce
1. Clone the repository and open the solution in Visual Studio.
2. Build the solution to ensure all dependencies are resolved.
3. In the SecondPlugin project, choose which method to test by commenting/uncommenting the relevant lines in the `OnLoad` method of the `CustomUndoEventIssuePlugin2` class.
4. Set the SecondPlugin project as the startup project.
5. Run the Solution and execute the TestAddTen command some number of times.
   1. Note that this command registers a custom undo event each time it is executed to maintain data outside of the Rhino document.
6. Undo the command using Rhino's undo functionality and observe the behavior of the custom undo events.