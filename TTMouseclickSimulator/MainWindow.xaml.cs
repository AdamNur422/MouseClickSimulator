﻿using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

using TTMouseclickSimulator.Core;
using TTMouseclickSimulator.Core.Actions;
using TTMouseclickSimulator.Core.ToontownRewritten.Environment;
using TTMouseclickSimulator.Project;

using FormsTaskDialog = System.Windows.Forms.TaskDialog;
using FormsTaskDialogButton = System.Windows.Forms.TaskDialogButton;
using FormsTaskDialogCommandLinkButton = System.Windows.Forms.TaskDialogCommandLinkButton;
using FormsTaskDialogExpander = System.Windows.Forms.TaskDialogExpander;
using FormsTaskDialogExpanderPosition = System.Windows.Forms.TaskDialogExpanderPosition;
using FormsTaskDialogIcon = System.Windows.Forms.TaskDialogIcon;
using FormsTaskDialogPage = System.Windows.Forms.TaskDialogPage;

namespace TTMouseclickSimulator;

public partial class MainWindow : Window
{
    private const string AppName = "TTR Mouse Click Simulator";

    private const string actionTitleMainAction = "Main Action";

    /// <summary>
    /// The file extension for Simulator Project files. Currently we use ".xml".
    /// </summary>
    private const string ProjectFileExtension = ".xml";
    private const string SampleProjectsFolderName = "SampleProjects";

    private readonly Microsoft.Win32.OpenFileDialog openFileDialog;

    private SimulatorProject? project;
    private SimulatorConfiguration.QuickActionDescriptor? currentQuickAction;
    private Button[]? quickActionButtons;

    private Simulator? simulator;

    // Callbacks that we need to call when we start or stop the simulator.
    private Action? simulatorStartAction, simulatorStopAction;

    /// <summary>
    /// If true, the window should be closed after the simulator stopped.
    /// </summary>
    private bool closeWindowAfterStop;

    public MainWindow()
    {
        this.InitializeComponent();

        this.lblAppName.Content = AppName;
        this.Title = AppName;

        this.openFileDialog = new Microsoft.Win32.OpenFileDialog();
        this.openFileDialog.DefaultExt = ProjectFileExtension;
        this.openFileDialog.Filter = "XML Simulator Project|*" + ProjectFileExtension;
        // Set the initial directory to the executable path or the "SampleProjects" folder if it exists.
        string exeDirectory = Path.GetDirectoryName(Environment.ProcessPath)!;
        string sampleProjectsPath = Path.Combine(exeDirectory, SampleProjectsFolderName);

        if (Directory.Exists(sampleProjectsPath))
            this.openFileDialog.InitialDirectory = sampleProjectsPath;
        else
            this.openFileDialog.InitialDirectory = exeDirectory;

        this.RefreshProjectControls();
    }

    private async void HandleBtnStartClick(object sender, RoutedEventArgs e)
    {
        await this.RunSimulatorAsync();
    }

    private void HandleBtnStopClick(object sender, RoutedEventArgs e)
    {
        // The simulator is set to null before this button is disabled. However,
        // normally it shouldn't be possible for the user to click it in this case
        // because we show a modal task dialog.
        this.simulator?.Cancel();
        this.btnStop.IsEnabled = false;
    }

    private void HandleWindowClosing(object sender, CancelEventArgs e)
    {
        // If the simulator is currently running, don't close the window but stop the
        // simulator and wait until it is finished.
        if (this.simulator is not null)
        {
            e.Cancel = true;
            this.Hide();

            this.closeWindowAfterStop = true;
            this.simulator.Cancel();
        }
    }

    private async ValueTask RunSimulatorAsync()
    {
        this.btnStart.IsEnabled = false;
        this.btnStop.IsEnabled = true;
        this.btnLoad.IsEnabled = false;
        this.chkEnableBackgroundMode.IsEnabled = false;

        if (this.quickActionButtons is not null)
        {
            foreach (var bt in this.quickActionButtons)
                bt.IsEnabled = false;
        }

        bool backgroundMode = this.chkEnableBackgroundMode.IsChecked == true;

        this.simulatorStartAction?.Invoke();

        var runException = default(Exception);
        try
        {
            // We need to create (and dispose) the simulator in the GUI thread because
            // we need to set it in an instance variable, and because the GUI thread may
            // call Simulator.Cancel() that has to access the CancellationTokenSource of
            // the StandardInteractionProvider.
            var environment = TTRWindowsEnvironment.Instance;
            using var sim = this.simulator = new Simulator(
                this.currentQuickAction is not null ?
                    this.currentQuickAction.Action! :
                    this.project!.Configuration.MainAction!,
                environment,
                backgroundMode);

            // Run the simulator in another task so it is not executed in the GUI
            // thread. However, we then await that new task so we are notified when
            // it is finished (and that continuation will be run in the GUI thread
            // again).
            await Task.Run(async () =>
            {
                sim.AsyncRetryHandler = this.HandleSimulatorRetryAsync;

                sim.SimulatorInitializing += multipleWindowsAvailable => this.Dispatcher.Invoke(new Action(() =>
                {
                    if (multipleWindowsAvailable is not null)
                    {
                        // Initialization has started, so show the overlay message.
                        this.overlayMessageBorder.Visibility = Visibility.Visible;

                        this.overlayMessageTextBlock.Text = multipleWindowsAvailable.Value ?
                            "Multiple Toontown windows detected. Please activate the " +
                            "window that the Simulator should use." :
                            "Waiting for the window to be activated…";
                    }
                    else
                    {
                        // Initialization has finished.
                        this.overlayMessageBorder.Visibility = Visibility.Hidden;
                    }
                }));

                await sim.RunAsync();
            });
        }
        catch (Exception ex)
        {
            runException = ex;
        }
        finally
        {
            this.simulator = null;
        }

        this.simulatorStopAction?.Invoke();

        // Don't show a messagebox if we need to close the window.
        if (!this.closeWindowAfterStop && runException is not null and not OperationCanceledException)
        {
            string? exceptionDetails = GetExceptionDetailsText(runException);

            var dialogPage = new FormsTaskDialogPage()
            {
                Caption = AppName,
                Heading = "Simulator stopped!",
                Text = runException.Message,
                Expander = exceptionDetails is null ? null : new FormsTaskDialogExpander()
                {
                    Text = exceptionDetails,
                    Position = FormsTaskDialogExpanderPosition.AfterFootnote
                },
                Icon = FormsTaskDialogIcon.Error,
                Buttons =
                {
                    FormsTaskDialogButton.OK
                }
            };

            FormsTaskDialog.ShowDialog(
                new WindowInteropHelper(this).Handle,
                dialogPage);
        }

        this.HandleSimulatorStopped();
    }

    private async ValueTask<bool> HandleSimulatorRetryAsync(Exception ex)
    {
        // Show a TaskDialog.
        bool result = false;
        await this.Dispatcher.InvokeAsync(new Action(() =>
        {
            // If we cancelled the simulator already in the meanwhile, it wouldn't make
            // sense to show the dialog. Otherwise, evewn if the user selects "Try again",
            // the simulator would still stop afterwards.
            // The same is applies when the window has already been hidden and we just
            // wait for the simulator to stop.
            if (this.simulator!.IsCancelled || this.closeWindowAfterStop)
                return;

            string? exceptionDetails = GetExceptionDetailsText(ex);

            var buttonTryAgain = new FormsTaskDialogCommandLinkButton(
                "Try again",
                "The Simulator will try to run the current action again.");

            var buttonStop = new FormsTaskDialogCommandLinkButton("Stop the Simulator");

            var dialogPage = new FormsTaskDialogPage()
            {
                Caption = AppName,
                Heading = "Simulator interrupted!",
                Text = ex.Message,
                Expander = exceptionDetails is null ? null : new FormsTaskDialogExpander()
                {
                    Text = exceptionDetails,
                    Position = FormsTaskDialogExpanderPosition.AfterFootnote
                },
                Icon = FormsTaskDialogIcon.Warning,
                Buttons =
                {
                    buttonTryAgain,
                    buttonStop,
                    FormsTaskDialogButton.Cancel,
                },
                DefaultButton = buttonStop
            };

            var resultButton = FormsTaskDialog.ShowDialog(
                new WindowInteropHelper(this).Handle,
                dialogPage);

            if (resultButton == buttonTryAgain)
                result = true;
        }));

        return result;
    }

    private static string? GetExceptionDetailsText(Exception ex)
    {
        var detailsSb = default(StringBuilder);
        var innerEx = ex.InnerException;

        while (innerEx is not null)
        {
            if (detailsSb is null)
                detailsSb = new StringBuilder();
            else
                detailsSb.Append("\n\n");

            detailsSb.Append(innerEx.Message);

            innerEx = innerEx.InnerException;
        }

        return detailsSb?.ToString();
    }

    private void HandleSimulatorStopped()
    {
        this.btnStart.IsEnabled = true;
        this.btnStop.IsEnabled = false;
        this.btnLoad.IsEnabled = true;
        this.chkEnableBackgroundMode.IsEnabled = true;

        if (this.quickActionButtons is not null)
        {
            foreach (var bt in this.quickActionButtons)
                bt.IsEnabled = true;
        }

        if (this.currentQuickAction is not null)
        {
            this.currentQuickAction = null;
            this.RefreshProjectControls();
        }

        if (this.closeWindowAfterStop)
            this.Close();
    }

    private void HandleBtnLoadClick(object sender, RoutedEventArgs e)
    {
        if (this.openFileDialog.ShowDialog(this) == true)
        {
            // Try to load the given project.
            try
            {
                using var fs = new FileStream(
                    this.openFileDialog.FileName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                this.project = XmlProjectDeserializer.Deserialize(fs);
            }
            catch (Exception ex)
            {
                string? exceptionDetails = GetExceptionDetailsText(ex);

                var dialogPage = new FormsTaskDialogPage()
                {
                    Caption = AppName,
                    Heading = "Could not load the selected project.",
                    Text = ex.Message,
                    Expander = exceptionDetails is null ? null : new FormsTaskDialogExpander()
                    {
                        Text = exceptionDetails,
                        Position = FormsTaskDialogExpanderPosition.AfterFootnote
                    },
                    Icon = FormsTaskDialogIcon.Error,
                    SizeToContent = true
                };

                FormsTaskDialog.ShowDialog(
                    new WindowInteropHelper(this).Handle,
                    dialogPage);

                return;
            }


            if (this.quickActionButtons is not null)
            {
                foreach (var b in this.quickActionButtons)
                    this.gridProjectControls.Children.Remove(b);

                this.quickActionButtons = null;
            }

            this.RefreshProjectControls();

            // For each quick action, create a button.
            this.quickActionButtons = new Button[this.project.Configuration!.QuickActions.Count];
            for (int idx = 0; idx < this.project.Configuration.QuickActions.Count; idx++)
            {
                int i = idx;
                var quickAction = this.project.Configuration.QuickActions[i];

                var b = this.quickActionButtons[i] = new Button();
                b.Height = 21;
                b.HorizontalAlignment = HorizontalAlignment.Left;
                b.VerticalAlignment = VerticalAlignment.Top;
                b.Margin = new Thickness(0, 2 + 23 * i, 0, 0);
                b.Content = "  " + quickAction.Name + "  ";
                this.gridProjectControls.Children.Add(b);
                Grid.SetRow(b, 1);

                b.Click += async (_s, _e) =>
                {
                    this.currentQuickAction = quickAction;
                    this.RefreshProjectControls();

                    await this.RunSimulatorAsync();
                };
            }
        }
    }

    private void RefreshProjectControls()
    {
        this.lblActionTitle.Content = this.currentQuickAction is not null ? this.currentQuickAction.Name :
            this.project?.Configuration.MainAction is not null ? actionTitleMainAction : "";

        if (this.project is null)
        {
            this.lblCurrentProject.Content = "(none)";
            this.txtDescription.Text = string.Empty;
            this.btnStart.IsEnabled = false;
        }
        else
        {
            this.lblCurrentProject.Content = this.project.Title;
            this.txtDescription.Text = this.project.Description;
            this.btnStart.IsEnabled = this.project.Configuration.MainAction is not null;

            // Create labels for each action.
            this.actionListGrid.Children.Clear();
            var mainAct = this.currentQuickAction is not null ? this.currentQuickAction.Action
                : this.project.Configuration.MainAction;

            if (mainAct is not null)
            {
                int posCounter = 0;
                this.CreateActionLabels(
                    mainAct,
                    this.actionListGrid,
                    0,
                    ref posCounter,
                    out this.simulatorStartAction,
                    out this.simulatorStopAction);
            }
        }
    }

    private void HandleChkEnableBackgroundModeChecked(object sender, RoutedEventArgs e)
    {
        this.textBlockStopSimulatorNote.Visibility = Visibility.Collapsed;
    }

    private void HandleChkEnableBackgroundModeUnchecked(object sender, RoutedEventArgs e)
    {
        this.textBlockStopSimulatorNote.Visibility = Visibility.Visible;
    }

    private void CreateActionLabels(
        IAction action,
        Grid grid,
        int recursiveCount,
        ref int posCounter,
        out Action handleStart,
        out Action handleStop)
    {
        var l = new Label();
        l.Margin = new Thickness(recursiveCount * 10, 18 * posCounter, 0, 0);
        grid.Children.Add(l);

        string str = action.ToString()!;
        l.Content = str;

        handleStart = () =>
        {
            l.Foreground = new SolidColorBrush(Color.FromArgb(255, 29, 134, 184));
        };

        handleStop = () =>
        {
            l.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            l.Content = str;
        };

        action.ActionInformationUpdated += s => this.Dispatcher.Invoke(new Action(() => l.Content = str + " – " + s));

        posCounter++;

        if (action is IActionContainer)
        {
            var cont = (IActionContainer)action;
            var subActions = cont.SubActions;
            var handleStartActions = new Action[subActions.Count];
            var handleStopActions = new Action[subActions.Count];

            for (int i = 0; i < subActions.Count; i++)
            {
                this.CreateActionLabels(
                    subActions[i],
                    grid,
                    recursiveCount + 1,
                    ref posCounter,
                    out handleStartActions[i],
                    out handleStopActions[i]);
            }

            int? currentActiveAction = null;
            cont.SubActionStartedOrStopped += (idx) => this.Dispatcher.Invoke(new Action(() =>
            {
                if (idx.HasValue)
                {
                    currentActiveAction = idx;
                    handleStartActions[idx.Value]();
                }
                else if (currentActiveAction.HasValue)
                {
                    handleStopActions[currentActiveAction.Value]();
                }
            }));
        }
    }
}
