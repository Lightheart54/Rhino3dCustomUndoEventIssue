using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace CustomUndoEventIssue;

public class TestBalanceHolder
{
    public double Balance { get; set; } = 0.0;

    private static TestBalanceHolder g_balance_holder;

    /// <summary>
    /// Gets the one and only instance of this class
    /// </summary>
    public static TestBalanceHolder Instance
    {
        get
        {
            if (null == g_balance_holder)
                g_balance_holder = new TestBalanceHolder();
            return g_balance_holder;
        }
    }
}

public static class TestCustomUndoHandler
{
    /// <summary>
    /// Undo the change to TestBalanceHolder.Balance
    /// The Rhino Undo command will call this custom undo handling method
    /// when the event needs to be undone. You should NEVER change any setting
    /// in the Rhino document or application. Rhino handles ALL changes to the
    /// application and document and you will break the Undo/Redo commands if 
    /// you make any changes to the application or document.
    /// </summary>
    public static void OnCustomUndo(object sender, CustomUndoEventArgs e)
    {
        double undoAmount = (double)e.Tag;
        var balance = TestBalanceHolder.Instance.Balance;
        balance -= undoAmount;
        TestBalanceHolder.Instance.Balance = balance;
        RhinoApp.WriteLine("New balance: {0}", balance);
        e.Document.AddCustomUndoEvent("Redo " + e.ActionDescription, OnCustomUndo, -undoAmount);
    }
}

/// <summary>
/// TestEarnTen command
/// </summary>
public class TestEarnTen : Command
{
    public override string EnglishName => "TestEarnTen";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        const double amount = 10.0;
        doc.AddCustomUndoEvent(EnglishName, TestCustomUndoHandler.OnCustomUndo, amount);

        var balance = TestBalanceHolder.Instance.Balance;
        balance += amount;
        TestBalanceHolder.Instance.Balance = balance;
        RhinoApp.WriteLine("New balance: {0}", balance);

        return Result.Success;
    }
}

/// <summary>
/// TestSpendFive command
/// </summary>
public class TestSpendFive : Command
{
    public override string EnglishName => "TestSpendFive";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        const double amount = -5.0;
        doc.AddCustomUndoEvent(EnglishName, TestCustomUndoHandler.OnCustomUndo, amount);

        var balance = TestBalanceHolder.Instance.Balance;
        balance += amount;
        TestBalanceHolder.Instance.Balance = balance;
        RhinoApp.WriteLine("New balance: {0}", balance);

        return Result.Success;
    }
}
