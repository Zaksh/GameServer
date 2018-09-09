using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IniParser;
using IniParser.Model;
using LeagueSandbox.GameServer.Logging;
using log4net;
using LeagueSandbox.GameServer.Exceptions;
using Newtonsoft.Json.Linq;

namespace LeagueSandbox.GameServer.Content
{
    public class ContentManager
    {
        private readonly ILog _logger;
        private Game _game;

        private Dictionary<string, SpellData> _spellData = new Dictionary<string, SpellData>();
        private Dictionary<string, CharData> _charData = new Dictionary<string, CharData>();

        public Dictionary<string, ContentFile> Content = new Dictionary<string, ContentFile>();

        private string _contentPath;
        public string GameModeName { get; }

        private ContentManager(Game game, string gameModeName, string contentPath)
        {
            _contentPath = contentPath;
            _game = game;
            _logger = LoggerProvider.GetLogger();

            GameModeName = gameModeName;
        }

        public string GetMapConfigPath(int mapId)
        {
            var path = Path.Combine(_contentPath, GameModeName, "LEVELS", $"Map{mapId}", $"Map{mapId}.json");
            if (!File.Exists(path))
            {
                throw new ContentNotFoundException($"Map configuration for Map {mapId} was not found in the content.");
            }

            return path;
        }

        public string GetUnitStatPath(string model)
        {
            var path = Path.Combine("DATA", "Characters", model, $"{model}.ini");
            if (!Content.ContainsKey(path))
            {
                throw new ContentNotFoundException($"Stat file for {model} was not found.");
            }

            return path;
        }

        public string GetSpellDataPath(string model, string spellName)
        {
            var possibilities = new[]
            {
                Path.Combine("DATA", "Characters", model, "Spells", $"{spellName}.ini"),
                Path.Combine("DATA", "Shared", "Spells", $"{spellName}.ini"),
                Path.Combine("DATA", "Spells", $"{spellName}.ini")
            };

            foreach (var path in possibilities)
            {
                if (Content.ContainsKey(path))
                {
                    return path;
                }
            }

            throw new ContentNotFoundException($"Spell data for {spellName} was not found.");
        }

        public SpellData GetSpellData(string champ, string spellName)
        {
            if (_spellData.ContainsKey(spellName))
            {
                return _spellData[spellName];
            }

            _spellData[spellName] = new SpellData(_game);
            _spellData[spellName].Load(champ, spellName);
            return _spellData[spellName];
        }

        public CharData GetCharData(string charName)
        {
            if (_charData.ContainsKey(charName))
            {
                return _charData[charName];
            }

            _charData[charName] = new CharData(_game);
            _charData[charName].Load(charName);
            return _charData[charName];
        }

        public static ContentManager LoadGameMode(Game game, string gameModeName, string contentPath)
        {
            var contentManager = new ContentManager(game, gameModeName, contentPath);
            var iniParser = new FileIniDataParser();
            foreach (var file in GameServerCore.Extensions.GetAllFilesInDirectory(contentPath,
                x => x.EndsWith(".ini")))
            {
                var relativePath = file.Replace(contentPath, "").Replace(gameModeName, "").Substring(2);
                contentManager.Content[relativePath] = new ContentFile(iniParser.ReadFile(file));
                contentManager._logger.Debug($"Mapped Content [{relativePath}]");
            }

            return contentManager;
        }

        private static bool ValidatePackageName(string packageName)
        {
            if (packageName.Equals("Self"))
            {
                return true;
            }

            if (!packageName.Contains('-'))
            {
                return false;
            }

            var parts = packageName.Split('-');
            foreach (var part in parts)
            {
                if (part.Length < 2)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
