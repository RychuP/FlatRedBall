﻿using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using GameCommunicationPlugin.Common;
using GameJsonCommunicationPlugin.Common;
using Newtonsoft.Json;
using GameCommunicationPlugin.GlueControl.CodeGeneration;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using GameCommunicationPlugin.CodeGeneration;
using ToolsUtilities;
using GameCommunicationPlugin.GlueControl.ViewModels;
using OfficialPlugins.Compiler.CommandReceiving;
using System.Collections.Generic;
using GameCommunicationPlugin.GlueControl;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;

namespace GameCommunicationPlugin
{
    [Export(typeof(PluginBase))]
    public class MainGameCommunicationPlugin : PluginBase
    {
        #region Fields/Properties

        private GameConnectionManager _gameCommunicationManager;
        private Game1GlueCommunicationGenerator game1GlueCommunicationGenerator;
        private ApplyEditorCommandsCodeGenerator applyEditorCommandsCodeGenerator;

        public override string FriendlyName => "Game Communication Plugin";

        public override Version Version => new Version(1, 0);

        public static MainGameCommunicationPlugin Self { get; private set; }

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            _gameCommunicationManager.OnPacketReceived -= HandleOnPacketReceived;
            _gameCommunicationManager.Dispose();
            _gameCommunicationManager = null;
            GameConnectionManager.Self = null;
            return true;
        }

        public override void StartUp()
        {
            Self = this;
            _gameCommunicationManager = new GameConnectionManager(ReactToPluginEvent);
            _gameCommunicationManager.Port = 8888;
            _gameCommunicationManager.OnPacketReceived += HandleOnPacketReceived;
            GameConnectionManager.Self = _gameCommunicationManager;

            game1GlueCommunicationGenerator = new Game1GlueCommunicationGenerator(true, 8888);
            RegisterCodeGenerator(game1GlueCommunicationGenerator);

            applyEditorCommandsCodeGenerator = new ApplyEditorCommandsCodeGenerator();
            RegisterCodeGenerator(applyEditorCommandsCodeGenerator);

            //Task.Run(() =>
            //{
            //    while (true)
            //    {
            //        Thread.Sleep(500);
            //        _gameCommunicationManager?.SendItem(new GameConnectionManager.Packet
            //        {
            //            PacketType = "Test",
            //            Payload = DateTime.Now.ToLongTimeString(),
            //        });
            //    }
            //});
        }

        public void SetPrimarySettings(int portNumber, bool doConnections)
        {
            _gameCommunicationManager.Port = portNumber;
            _gameCommunicationManager.DoConnections = doConnections;
            game1GlueCommunicationGenerator.PortNumber = _gameCommunicationManager.Port;
            game1GlueCommunicationGenerator.IsGameCommunicationEnabled = _gameCommunicationManager.DoConnections;

            applyEditorCommandsCodeGenerator.IsGameCommunicationEnabled = _gameCommunicationManager.DoConnections;
        }

        private async Task<object> HandleOnPacketReceived(GameConnectionManager.Packet packet)
        {
            object toReturn = null;
            if(!string.IsNullOrEmpty(packet.Payload))
            {
                //ReactToPluginEvent($"GameCommunicationPlugin_PacketReceived_{packetReceivedArgs.Packet.PacketType}", packetReceivedArgs.Packet.Payload);

                // do we want to await this?
                toReturn = await MainCompilerPlugin.Self.CommandReceiver.HandleCommandsFromGame(packet.Payload, _gameCommunicationManager.Port);

                Debug.WriteLine($"Packet Type: {packet.PacketType}, Payload: {packet.Payload}");
            }
            return toReturn;
        }

        public void AddReflectionStateAssignmentCode(ICodeBlock codeBlock, StateSaveCategory category)
        {
            ReflectionBasedStateCodeGenerator.AddReflectionStateAssignmentCode(codeBlock, category);
        }
    }
}
