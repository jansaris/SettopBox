using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EpgGrabber.Models;
using log4net;

namespace EpgGrabber
{
    public class GenreTranslator
    {
        readonly ILog _logger;
        readonly Settings _settings;

        private const string Language = "TVH";
        private readonly List<Tuple<string, string>> _translations = new List<Tuple<string, string>>();

        public GenreTranslator(ILog logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public void Load()
        {
            var filename = Path.Combine(_settings.DataFolder, _settings.EpgTranslationsFile);
            if (!File.Exists(filename))
            {
                _logger.Warn($"Translation file {filename} doesn't exist");
                return;
            }
            try
            {
                _logger.Debug($"Load {filename}");
                var lines = File.ReadAllLines(filename);
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
                _logger.Error($"Failed to load {filename}", ex);
            }
        }

        public List<Genre> Translate(List<Genre> genres)
        {
            foreach (var genre in genres.Where(NoTranslation))
            {
                _logger.WarnFormat($"Failed to translate genre: {genre.Name}");
            }

            return genres
                .Where(AnyTranslation)
                .SelectMany(GetTranslations)
                .Select(translation => new Genre
                {
                    Language = Language,
                    Name = translation
                })
                .ToList();
        }

        private IEnumerable<string> GetTranslations(Genre genre)
        {
            return _translations
                .Where(t => t.Item1 == genre.Name)
                .Select(t => t.Item2)
                .ToList();
        }

        private bool AnyTranslation(Genre genre)
        {
            return _translations.Any(t => t.Item1 == genre.Name);
        }

        private bool NoTranslation(Genre genre)
        {
            return !AnyTranslation(genre);
        }
    }
}