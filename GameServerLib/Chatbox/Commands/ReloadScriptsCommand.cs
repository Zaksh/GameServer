﻿using ENet;

namespace LeagueSandbox.GameServer.Chatbox.Commands
{
    public class ReloadScriptsCommand : ChatCommandBase
    {
        public override string Command => "reloadscripts";
        public override string Syntax => $"{Command}";

        public ReloadScriptsCommand(ChatCommandManager chatCommandManager, Game game)
            : base(chatCommandManager, game)
        {
        }

        public override void Execute(Peer peer, bool hasReceivedArguments, string arguments = "")
        {
            if (Game.LoadScripts(true))
            {
                ChatCommandManager.SendDebugMsgFormatted(DebugMsgType.INFO, "Scripts reloaded.");
            }
            else
            {
                ChatCommandManager.SendDebugMsgFormatted(DebugMsgType.INFO, "Scripts failed to reload.");
            }
        }
    }
}
