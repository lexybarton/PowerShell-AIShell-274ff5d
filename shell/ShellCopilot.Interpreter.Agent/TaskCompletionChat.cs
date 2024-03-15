﻿using Azure.AI.OpenAI;
using Azure;
using Newtonsoft.Json;
using ShellCopilot.Abstraction;
using System;
using System.Runtime;
using System.Text;
using System.Diagnostics;
namespace ShellCopilot.Interpreter.Agent;
/// <summary>
/// Summary description for Class1
/// </summary>
internal class TaskCompletionChat
{
	private ChatService _chatService;
	private IHost host;
    private Computer computer;
    private Dictionary<string,string> prompts = TaskCompletionChatPrompts.prompts;
    private IModel model;
    private bool _isFunctionCallingModel;

	internal TaskCompletionChat(bool isFunctionCallingModel, ChatService chatService, IHost Host)
	{
		_chatService = chatService;
		host = Host;
        computer = new();
        _isFunctionCallingModel = isFunctionCallingModel;
        if(_isFunctionCallingModel)
        {
            model = new FunctionCallingModel(_chatService, host);
        }
        else
        {
            model = new TextBasedModel(_chatService, host);
        }
	}

    internal void CleanUpProcesses()
    {
        computer.Terminate();
    }

    public async Task<bool> StartTask(string input, RenderingStyle _renderingStyle, CancellationToken token)
    {
        bool chatCompleted = false;
        string previousCode = "";
        //input += prompts["Initial"];
        while (!chatCompleted)
        {
            if (string.IsNullOrEmpty(input))
            {
                break;
            }
            try
            {
                InternalChatResultsPacket packet = await model.SmartChat(input, _renderingStyle, token);

                PromptEngineering(ref input, ref chatCompleted, ref previousCode, packet);
            }
            catch (OperationCanceledException)
            {
                // Ignore the exception
            }
        }

        return chatCompleted;
    }

    private void PromptEngineering(ref string input, ref bool chatCompleted, ref string previousCode, InternalChatResultsPacket packet)
    {
        if(string.IsNullOrEmpty(input))
        {
            input = "";
            chatCompleted = true;
        }
        if (packet.wasCodeGiven && !packet.didNotCallTool)
        {
            if(packet.Code.Equals(previousCode) && !string.IsNullOrEmpty(previousCode))
            {
                input = prompts["SameError"];
            }
            else if(packet.didNotCallTool)
            {
                input = prompts["UseTool"];
            }
            else if (packet.wasToolSupported && packet.languageSupported)
            {
                if (packet.didUserRun)
                {
                    if (packet.wasToolCancelled)
                    {
                        input = prompts["ToolCancelled"];
                    }
                    else
                    {
                        if (packet.wasThereAnError)
                        {
                            input = prompts["Error"];
                        }
                        else
                        {
                            if (_isFunctionCallingModel)
                            {
                                input = prompts["OutputFunctionBased"];
                            }
                            else
                            {
                                input = prompts["OutputTextBased"] + packet.toolResponse;
                            }
                            previousCode = packet.Code;
                        }
                    }
                }
                else
                {
                    input = prompts["StopTask"];
                }
            }
            else
            {
                input = packet.toolResponse + prompts["Force"];
            }
        }
        else
        {
            if (packet.isTaskComplete)
            {
                //TODO: add a way to save the file
                chatCompleted = true;
                computer.Terminate();
            }
            else if (packet.isTaskImpossible)
            {
                //TODO: add a way to save the file
                chatCompleted = true;
                computer.Terminate();
            }
            else if (packet.isMoreInformationNeeded)
            {
                chatCompleted = true;
            }
            else if(packet.isNoTaskPresent)
            {
                chatCompleted = true;
            }
            else if (packet.didNotCallTool)
            {
                input = prompts["UseTool"];
            }
            else
            {
                input = prompts["Force"];
            }
        }
    }
}
