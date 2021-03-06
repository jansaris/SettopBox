﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using EpgGrabber.Models;
using log4net;

namespace EpgGrabber
{
    public sealed class XmlTv
    {
        XmlDocument _xml;
        readonly ILog _logger;
        readonly Settings _settings;

        public XmlTv(Settings settings, ILog logger)
        {
            _settings = settings;
            _logger = logger;
        }

        /// <summary>
        /// Generates the XMLtv file
        /// </summary>
        /// <param name="epgChannels">The epg channels.</param>
        public string GenerateXmlTv(List<Channel> epgChannels)
        {
            var fileName = Path.Combine(_settings.DataFolder, _settings.XmlTvFileName);
            _logger.Info($"Generating XMLTV file {fileName}");
            _xml = new XmlDocument();
            GenerateRoot();
            GenerateChannels(epgChannels);
            GeneratePrograms(epgChannels);

            //Save xml
            _xml.InsertBefore(_xml.CreateXmlDeclaration("1.0", "UTF-8", null), _xml.DocumentElement);
            var file = new FileInfo(fileName);
            _xml.Save(file.FullName);
            return file.FullName;
        }

        /// <summary>
        /// Generates the root node
        /// </summary>
        /// <returns></returns>
        void GenerateRoot()
        {
            var node = AppendNode(_xml, "tv");
            AppendAttribute(node, "generator-info-name", "GlashartEPGgrabber (by Dennieku & jansaris)");
        }

        /// <summary>
        /// Generates the channels.
        /// </summary>
        /// <param name="epgChannels"></param>
        void GenerateChannels(List<Channel> epgChannels)
        {
            var root = _xml.DocumentElement;
            var names = epgChannels.Select(c => c.Name).Distinct().ToList();

            //Loop through the channels
            foreach (var channel in names)
            {
                var channelNode = AppendNode(root, "channel");
                AppendAttribute(channelNode, "id", channel);
                var displayNode = AppendNode(channelNode, "display-name", channel);
                AppendAttribute(displayNode, "lang", "nl"); //just setting everything to NL
                //var icon = channel.Icons.FirstOrDefault(ico => File.Exists(Path.Combine(_settings.IconFolder, ico)));
                //if (icon == null) continue;

                ////<icon src="file://C:\Perl\site/share/xmltv/icons/KERA.gif" />
                //var file = new FileInfo(Path.Combine(_settings.IconFolder, icon));
                //var iconNode = AppendNode(channelNode, "icon");
                //AppendAttribute(iconNode, "src", file.FullName);
            }
        }

        /// <summary>
        /// Generates the programs.
        /// </summary>
        /// <param name="epgChannels">The epg channels.</param>
        void GeneratePrograms(List<Channel> epgChannels)
        {
            var root = _xml.DocumentElement;

            //Loop through the channels
            foreach (var channel in epgChannels)
            {
                //Loop through the programs
                foreach (var prog in channel.Programs)
                {
                    try
                    {
                        //Create the xml node
                        var progNode = AppendNode(root, "programme");
                        AppendAttribute(progNode, "start", prog.StartString);
                        AppendAttribute(progNode, "stop", prog.EndString);
                        AppendAttribute(progNode, "channel", channel.Name);
                        var titleNode = AppendNode(progNode, "title", prog.Name);
                        AppendAttribute(titleNode, "lang", "nl");
                        if (!string.IsNullOrWhiteSpace(prog.Description))
                        {
                            var descNode = AppendNode(progNode, "desc", prog.Description);
                            AppendAttribute(descNode, "lang", "nl");
                        }
                        if (prog.Genres == null || !prog.Genres.Any()) continue;

                        foreach (var genre in prog.Genres)
                        {
                            var categoryNode = AppendNode(progNode, "category", genre.Name);
                            AppendAttribute(categoryNode, "lang", genre.Language);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed on prog {prog.Id}", ex);
                    }
                }
            }
        }


        /// <summary>
        /// Appends the node.
        /// </summary>
        /// <param name="parent">The parent node</param>
        /// <param name="name">The name.</param>
        /// <param name="innerText"></param>
        /// <returns></returns>
        XmlNode AppendNode(XmlNode parent, string name, string innerText = null)
        {
            XmlNode node = _xml.CreateElement(name);
            parent.AppendChild(node);
            if (!string.IsNullOrWhiteSpace(innerText))
                node.InnerText = innerText;
            return node;
        }

        /// <summary>
        /// Appends the attribute.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        void AppendAttribute(XmlNode node, string name, string value)
        {
            var attr = _xml.CreateAttribute(name);
            attr.Value = value;
            node.Attributes?.Append(attr);
        }
    }
}
