using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Azure.Identity;
using ShellCopilot.Abstraction;

namespace ShellCopilot.Ollama.Agent;

public sealed class OllamaAgent : ILLMAgent
{
    public string Name => "ollama";
    public string Description => "This is an AI assistant that utilizes Ollama";
    public string Company => "Microsoft";
    public List<string> SampleQueries => [
        "How do I list files in a directory?"
    ];
    public Dictionary<string, string> LegalLinks { private set; get; } = null;

    private OllamaChatService _chatService;
    private StringBuilder _text; // Text to be rendered at the end

    public void Dispose()
    {
        _chatService?.Dispose();
    }

    public void Initialize(AgentConfig config)
    {
        _text = new StringBuilder();
        _chatService = new OllamaChatService();

        LegalLinks = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Ollama Docs"] = "https://github.com/ollama/ollama",
        };

    }

    public IEnumerable<CommandBase> GetCommands() => null;

    public string SettingFile { private set; get; } = null;

    public void RefreshChat() {}

    public bool CanAcceptFeedback(UserAction action) => false;
    
    public void OnUserAction(UserActionPayload actionPayload){}

    public async Task<bool> Chat(string input, IShell shell)
    {
        IHost host = shell.Host; // Get the shell host
        CancellationToken token = shell.CancellationToken; // get the cancelation token

        try
        {
            ResponseData ollama_Response = await host.RunWithSpinnerAsync(
                status: "Thinking ...",
                func: async context => await _chatService.GetChatResponseAsync(context, input, token)
            ).ConfigureAwait(false);

            if (ollama_Response is not null)
            {
                _text.AppendLine("Data: ").AppendLine(ollama_Response.response);
                host.RenderFullResponse(_text.ToString());
            }
        }
        catch (Exception e)
        {
            _text.AppendLine(e.ToString());

            host.RenderFullResponse(_text.ToString());
            
            return false;
        }
       

        return true;
    }
}
