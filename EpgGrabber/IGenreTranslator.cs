using System.Collections.Generic;
using EpgGrabber.Models;

namespace EpgGrabber
{
    public interface IGenreTranslator
    {
        List<EpgGenre> Translate(List<EpgGenre> genres);
    }
}