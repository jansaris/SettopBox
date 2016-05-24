using System.Collections.Generic;
using EpgGrabber.Models;

namespace EpgGrabber
{
    public interface IGenreTranslator
    {
        List<Genre> Translate(List<Genre> genres);
    }
}