using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EpgGrabber.Models;
using log4net;

namespace EpgGrabber
{
    public class TvhGenreTranslator : IGenreTranslator
    {
        readonly ILog _logger;
        readonly Settings _settings;

        private const string Language = "TVH";
        private readonly List<Tuple<string, string>> _translations = new List<Tuple<string, string>>();

        public TvhGenreTranslator(ILog logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public void Load()
        {
            if (!File.Exists(_settings.EpgTranslationsFile))
            {
                _logger.WarnFormat("Translation file {0} doesn't exist", _settings.EpgTranslationsFile);
                return;
            }
            try
            {
                _logger.DebugFormat("Load {0}", _settings.EpgTranslationsFile);
                var lines = File.ReadAllLines(_settings.EpgTranslationsFile);
                foreach (var line in lines)
                {
                    var splitted = line.Split(';');
                    if (splitted.Length < 2)
                    {
                        _logger.Warn($"Failed to convert line as Genre translation: {line}");
                        continue;
                    }
                    var glashartGenre = splitted.First();
                    foreach (var tvhGenre in splitted.Skip(1))
                    {
                        _logger.Debug($"Add translation: GH {glashartGenre} -- TVH {tvhGenre}");
                        _translations.Add(new Tuple<string, string>(glashartGenre, tvhGenre));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load {_settings.EpgTranslationsFile}", ex);
            }
        }

        public List<EpgGenre> Translate(List<EpgGenre> genres)
        {
            foreach (var genre in genres.Where(NoTranslation))
            {
                _logger.WarnFormat("Failed to translate genre: {0}", genre.Genre);
            }

            return genres
                .Where(AnyTranslation)
                .SelectMany(GetTranslations)
                .Select(translation => new EpgGenre
                {
                    Language = Language,
                    Genre = translation
                })
                .ToList();
        }

        private IEnumerable<string> GetTranslations(EpgGenre genre)
        {
            return _translations
                .Where(t => t.Item1 == genre.Genre)
                .Select(t => t.Item2)
                .ToList();
        }

        private bool AnyTranslation(EpgGenre genre)
        {
            return _translations.Any(t => t.Item1 == genre.Genre);
        }

        private bool NoTranslation(EpgGenre genre)
        {
            return !AnyTranslation(genre);
        }
    }
}