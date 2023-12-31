using System.Reflection;
using Microsoft.PowerShell;
using ShellCopilot.Abstraction;
using ShellCopilot.Kernel.Commands;
using Spectre.Console;

namespace ShellCopilot.Kernel;

internal sealed class Shell : IShell
{
    private readonly bool _isInteractive;
    private readonly List<LLMAgent> _agents;
    private readonly Stack<LLMAgent> _activeAgentStack;
    private CancellationTokenSource _cancellationSource;

    private LLMAgent ActiveAgent => _activeAgentStack.TryPeek(out var agent) ? agent : null;
    internal bool Exit { set; get; }
    internal Host Host { get; }
    internal CommandRunner CommandRunner { get; }
    internal CancellationToken CancellationToken => _cancellationSource.Token;

    #region IShell implementation

    IHost IShell.Host => Host;
    CancellationToken IShell.CancellationToken => _cancellationSource.Token;

    #endregion IShell implementation

    /// <summary>
    /// Creates an instance of <see cref="Shell"/>.
    /// </summary>
    internal Shell(bool interactive, bool useAlternateBuffer = false, string historyFileNamePrefix = null)
    {
        _isInteractive = interactive;
        _agents = new List<LLMAgent>();
        _activeAgentStack = new Stack<LLMAgent>();
        _cancellationSource = new CancellationTokenSource();

        Exit = false;
        Host = new Host();

        if (interactive)
        {
            Host.WriteLine("Shell Copilot (v0.1)\n");
            CommandRunner = new CommandRunner(this);
            SetReadLineExperience();
        }

        LoadAvailableAgents();
        Console.CancelKeyPress += OnCancelKeyPress;

        if (interactive)
        {
            // Write out information about the active agent.
            var current = ActiveAgent?.Impl;
            if (current is not null)
            {
                Host.MarkupLine($"Using the agent [green]{current.Name}[/]:\n[italic]{current.Description.EscapeMarkup()}[/]\n");
            }

            // Write out help.
            Host.MarkupLine($"Type {ConsoleRender.FormatInlineCode("/help")} for instructions.")
                .WriteLine();
        }
    }

    /// <summary>
    /// Get all code blocks from the last LLM response.
    /// </summary>
    /// <returns></returns>
    internal List<string> GetCodeBlockFromLastResponse()
    {
        return Host.MarkdownRender.GetAllCodeBlocks();
    }

    /// <summary>
    /// Load a plugin assembly file and process the agents defined in it.
    /// </summary>
    internal void ProcessAgentPlugin(string pluginFile)
    {
        Assembly plugin = Assembly.LoadFrom(pluginFile);
        foreach (Type type in plugin.ExportedTypes)
        {
            if (!typeof(ILLMAgent).IsAssignableFrom(type))
            {
                continue;
            }

            var agent = (ILLMAgent)Activator.CreateInstance(type);
            var agentHome = Path.Join(Utils.AgentConfigHome, agent.Name);
            var config = new AgentConfig
            {
                IsInteractive = _isInteractive,
                ConfigurationRoot = Directory.CreateDirectory(agentHome).FullName,
                RenderingStyle = Console.IsOutputRedirected
                    ? RenderingStyle.FullResponsePreferred
                    : RenderingStyle.StreamingResponsePreferred
            };

            agent.Initialize(config);
            _agents.Add(new LLMAgent(agent));
        }
    }

    /// <summary>
    /// Load all available agents.
    /// </summary>
    private void LoadAvailableAgents()
    {
        //while (!System.Diagnostics.Debugger.IsAttached)
        //{
        //    Thread.Sleep(200);
        //}
        //System.Diagnostics.Debugger.Break();

        foreach (string dir in Directory.EnumerateDirectories(Utils.AgentHome))
        {
            string name = Path.GetFileName(dir);
            string file = Path.Join(dir, $"{name}.dll");

            if (!File.Exists(file))
            {
                continue;
            }

            try
            {
                ProcessAgentPlugin(file);
            }
            catch (Exception ex)
            {
                Host.MarkupErrorLine($"Failed to load the agent '{name}': {ex.Message}\n");
            }
        }

        if (_agents.Count is 0)
        {
            Host.MarkupWarningLine($"No agent available.\n");
            return;
        }

        try
        {
            LLMAgent chosenAgent = Host
                .PromptForSelectionAsync(
                    title: "[orange1]Please select an [Blue]agent[/] to use[/]:",
                    choices: _agents,
                    converter: static a => a.Impl.Name)
                .GetAwaiter().GetResult();

            PushActiveAgent(chosenAgent);
        }
        catch (Exception)
        {
            // Ignore failure from showing the confirmation prompt.
        }
    }

    /// <summary>
    /// Push the active agent on to the stack.
    /// </summary>
    internal void PushActiveAgent(LLMAgent agent)
    {
        if (_activeAgentStack.Count is 2)
        {
            throw new InvalidOperationException("Cannot push when two agents are already on stack.");
        }

        bool loadCommands = true;
        if (_activeAgentStack.TryPeek(out var current))
        {
            loadCommands = current != agent;
        }

        _activeAgentStack.Push(agent);
        if (loadCommands)
        {
            ILLMAgent impl = agent.Impl;
            CommandRunner.UnloadAgentCommands();
            CommandRunner.LoadCommands(impl.GetCommands(), impl.Name);
        }
    }

    /// <summary>
    /// Pop the current active agent off the stack.
    /// </summary>
    internal void PopActiveAgent()
    {
        if (_activeAgentStack.Count != 2)
        {
            throw new InvalidOperationException("Cannot pop when only one active agent is on stack.");
        }

        LLMAgent pre = _activeAgentStack.Pop();
        LLMAgent cur = _activeAgentStack.Peek();

        if (pre != cur)
        {
            ILLMAgent impl = cur.Impl;
            CommandRunner.UnloadAgentCommands();
            CommandRunner.LoadCommands(impl.GetCommands(), impl.Name);
        }
    }

    /// <summary>
    /// Switch and use another agent as the active.
    /// </summary>
    internal void SwitchActiveAgent(LLMAgent agent)
    {
        ILLMAgent impl = agent.Impl;
        if (_activeAgentStack.TryPop(out var current))
        {
            _activeAgentStack.Clear();
            _activeAgentStack.Push(agent);
            if (current != agent)
            {
                CommandRunner.UnloadAgentCommands();
                CommandRunner.LoadCommands(impl.GetCommands(), impl.Name);
            }
        }
        else
        {
            _activeAgentStack.Push(agent);
            CommandRunner.LoadCommands(impl.GetCommands(), impl.Name);
        }
    }

    /// <summary>
    /// For reference:
    /// https://github.com/dotnet/command-line-api/blob/67df30a1ac4152e7f6278847b88b8f1ea1492ba7/src/System.CommandLine/Invocation/ProcessTerminationHandler.cs#L73
    /// TODO: We may want to implement `OnPosixSignal` too for more reliable cancellation on non-Windows.
    /// </summary>
    private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
    {
        // Set the Cancel property to true to prevent the process from terminating.
        args.Cancel = true;
        switch (args.SpecialKey)
        {
            // Treat both Ctrl-C and Ctrl-Break as the same.
            case ConsoleSpecialKey.ControlC:
            case ConsoleSpecialKey.ControlBreak:
                // Request cancellation and refresh the cancellation source.
                _cancellationSource.Cancel();
                _cancellationSource = new CancellationTokenSource();
                return;
        }
    }

    /// <summary>
    /// Configure the read-line experience.
    /// </summary>
    private void SetReadLineExperience()
    {
        PSConsoleReadLineOptions options = PSConsoleReadLine.GetOptions();
        options.RenderHelper = new ReadLineHelper(CommandRunner);

        PSConsoleReadLine.SetKeyHandler(
            new[] { "Ctrl+d,Ctrl+c" },
            (key, arg) =>
            {
                PSConsoleReadLine.RevertLine();
                PSConsoleReadLine.Insert("/code copy");
                PSConsoleReadLine.AcceptLine();
            },
            "CopyCode",
            "Copy the code snippet from the last response to clipboard.");
    }

    /// <summary>
    /// Execute a command.
    /// </summary>
    /// <param name="input">Command line to be executed.</param>
    private void RunCommand(string input)
    {
        string commandLine = input[1..].Trim();
        if (commandLine == string.Empty)
        {
            Host.MarkupLine(ConsoleRender.FormatError("Command is missing."));
            return;
        }

        try
        {
            CommandRunner.InvokeCommand(commandLine);
        }
        catch (Exception e)
        {
            Host.MarkupLine(ConsoleRender.FormatError(e.Message));
        }
    }

    /// <summary>
    /// Run a chat REPL.
    /// </summary>
    internal async Task RunREPLAsync()
    {
        int count = 1;
        while (!Exit)
        {
            LLMAgent agent = ActiveAgent;
            string prompt = $"[bold green]aish[/]:{count}> ";
            Host.Markup(prompt);

            try
            {
                string input = PSConsoleReadLine.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                count++;
                if (input.StartsWith('/'))
                {
                    RunCommand(input);
                    continue;
                }

                // Now it's a query to the LLM.
                if (agent is null)
                {
                    // No agent to serve the query. Print the warning and go back to read-line prompt.
                    string agentCommand = ConsoleRender.FormatInlineCode($"/agent use");
                    string helpCommand = ConsoleRender.FormatInlineCode("/help");

                    Host.WriteLine()
                        .MarkupLine(ConsoleRender.FormatWarning("No active agent selected, chat is disabled."))
                        .MarkupLine(ConsoleRender.FormatWarning($"Run {agentCommand} to select an agent. Type {helpCommand} for more instructions."))
                        .WriteLine();
                    continue;
                }

                if (agent.IsOrchestrator(out IOrchestrator orchestrator)
                    && _activeAgentStack.Count is 1
                    && !agent.OrchestratorRoleDisabled
                    && _agents.Count > 1)
                {
                    Host.MarkupLine($"The active agent [green]{agent.Impl.Name}[/] can act as an orchestrator and there are multiple agents available.");
                    bool confirmed = await Host.PromptForConfirmationAsync(
                        prompt: $"Do you want it to find the most suitable agent for your query?",
                        defaultValue: false);

                    if (confirmed)
                    {
                        List<string> descriptions = new(capacity: _agents.Count);
                        foreach (LLMAgent item in _agents)
                        {
                            descriptions.Add(item.Impl.Description);
                        }

                        try
                        {
                            Task<int> find_agent_op() => orchestrator.FindAgentForPrompt(
                                prompt: input,
                                agents: descriptions,
                                token: CancellationToken).WaitAsync(CancellationToken);

                            int selected = await Host.RunWithSpinnerAsync(find_agent_op, status: "Thinking...");
                            string agentCommand = ConsoleRender.FormatInlineCode($"/agent pop");

                            if (selected >= 0)
                            {
                                var selectedAgent = _agents[selected];
                                PushActiveAgent(selectedAgent);
                                Host.MarkupLine(ConsoleRender.FormatNote($"Selected agent: [green]{selectedAgent.Impl.Name}[/]"))
                                    .MarkupLine(ConsoleRender.FormatNote($"It's now active for your query. When you are done with the topic, run {agentCommand} to return to the orchestrator."));
                            }
                            else
                            {
                                PushActiveAgent(agent);
                                Host.MarkupLine(ConsoleRender.FormatNote($"No suitable agent was found. The active agent [green]{agent.Impl.Name}[/] will be used for the topic."))
                                    .MarkupLine(ConsoleRender.FormatNote($"When you are done with the topic, run {agentCommand} to return to the orchestrator."));
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is OperationCanceledException)
                            {
                                // User cancelled the operation, so we return to the read-line prompt.
                                continue;
                            }

                            agent.OrchestratorRoleDisabled = true;
                            Host.WriteLine()
                                .MarkupLine(ConsoleRender.FormatError($"Operation failed: {ex.Message}"))
                                .WriteLine()
                                .MarkupLine(ConsoleRender.FormatNote($"The orchestrator role is disabled due to the failure. Continue with the active agent [green]{agent.Impl.Name}[/] for the query."));
                        }
                    }
                }

                try
                {
                    // Use the current active agent for the query.
                    agent = ActiveAgent;

                    // TODO: Consider `WaitAsync(CancellationToken)` to handle an agent not responding to ctr+c.
                    // One problem to use `WaitAsync` is to make sure we give reasonable time for the agent to handle the cancellation.
                    bool wasQueryServed = await agent.Impl.Chat(input, this);
                    if (!wasQueryServed)
                    {
                        Host.WriteLine()
                            .MarkupWarningLine($"[[{Utils.AppName}]]: Agent self-check failed. Resolve the issue as instructed and try again.")
                            .MarkupWarningLine($"[[{Utils.AppName}]]: Run {ConsoleRender.FormatInlineCode($"/agent config {agent.Impl.Name}")} to edit the settings for the agent.")
                            .WriteLine();
                    }
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                    {
                        // User cancelled the operation, so we return to the read-line prompt.
                        continue;
                    }

                    Host.WriteErrorLine()
                        .MarkupErrorLine($"Agent failed to generate a response: {ex.Message}")
                        .WriteErrorLine();
                }
            }
            catch (ShellCopilotException e)
            {
                AnsiConsole.MarkupLine(ConsoleRender.FormatError(e.Message));
                if (e.HandlerAction is ExceptionHandlerAction.Stop)
                {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Run a one-time chat.
    /// </summary>
    /// <param name="prompt"></param>
    internal async Task RunOnceAsync(string prompt)
    {
        if (ActiveAgent is null)
        {
            string settingCommand = ConsoleRender.FormatInlineCode($"{Utils.AppName} --settings");
            string helpCommand = ConsoleRender.FormatInlineCode($"{Utils.AppName} --help");

            Host.MarkupErrorLine($"No active agent was configured.");
            Host.MarkupErrorLine($"Run {settingCommand} to configure the active agent. Run {helpCommand} for details.");

            return;
        }

        try
        {
            await ActiveAgent.Impl.Chat(prompt, this).WaitAsync(CancellationToken);
        }
        catch (OperationCanceledException)
        {
            Host.MarkupErrorLine("Operation was aborted.");
        }
        catch (ShellCopilotException exception)
        {
            Host.MarkupErrorLine(exception.Message.EscapeMarkup());
        }
    }
}
