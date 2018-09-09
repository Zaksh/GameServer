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
        private Dictionary<string, NavGrid> _navGrids = new Dictionary<string, NavGrid>();

        public Dictionary<string, ContentFile> Content = new Dictionary<string, ContentFile>();
        public List<string> CSharpScriptFiles = new List<string>();

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

        public NavGrid GetNavGrid(string navGridPath)
        {
            if (_navGrids.ContainsKey(navGridPath))
            {
                return _navGrids[navGridPath];
            }

            throw new ContentNotFoundException($"NavGrid with path {navGridPath} was not loaded.");
        }

        public static ContentManager LoadGameMode(Game game, string gameModeName, string contentPath)
        {
            var contentManager = new ContentManager(game, gameModeName, contentPath);
            var iniParser = new FileIniDataParser();
            foreach (var file in Directory.GetFiles(contentPath, "*.*", SearchOption.AllDirectories))
            {
                var relativePath = file.Replace(contentPath, "").Replace(gameModeName, "").Substring(2);
                if (file.EndsWith(".ini"))
                {
                    var ini = iniParser.ReadFile(file);
                    contentManager.Content[relativePath] = new ContentFile(ParseIniFile(ini));
                }
                else if (file.EndsWith(".cs"))
                {
                    if (relativePath.StartsWith("bin") || relativePath.StartsWith("obj"))
                    {
                        continue;
                    }

                    contentManager.CSharpScriptFiles.Add(file);
                }
                else if (file.EndsWith(".aimesh_ngrid"))
                {
                    contentManager._navGrids[relativePath] = NavGridReader.ReadBinary(file);
                }
                else
                {
                    continue;
                }

                contentManager._logger.Debug($"Mapped Content [{relativePath}]");
            }

            return contentManager;
        }

        public static Dictionary<string, Dictionary<string, string>> ParseIniFile(IniData data)
        {
            var ret = new Dictionary<string, Dictionary<string, string>>();
            foreach (var section in data.Sections)
            {
                if (!ret.ContainsKey(section.SectionName))
                {
                    ret[section.SectionName] = new Dictionary<string, string>();
                }
                foreach (var field in section.Keys)
                {
                    ret[section.SectionName][field.KeyName] = field.Value;
                }
            }

            return ret;
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
