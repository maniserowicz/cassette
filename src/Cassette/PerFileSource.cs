﻿#region License
/*
Copyright 2011 Andrew Davey

This file is part of Cassette.

Cassette is free software: you can redistribute it and/or modify it under the 
terms of the GNU General Public License as published by the Free Software 
Foundation, either version 3 of the License, or (at your option) any later 
version.

Cassette is distributed in the hope that it will be useful, but WITHOUT ANY 
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS 
FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with 
Cassette. If not, see http://www.gnu.org/licenses/.
*/
#endregion

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cassette.IO;
using Cassette.Utilities;
using System.IO;

namespace Cassette
{
    public class PerFileSource<T> : IModuleSource<T>
        where T : Module
    {
        public PerFileSource(string basePath)
        {
            SearchOption = SearchOption.AllDirectories;
            if (basePath.StartsWith("~"))
            {
                this.basePath = basePath.TrimEnd('/', '\\');
            }
            else if (basePath.StartsWith("/"))
            {
                this.basePath = "~";
            }
            else
            {
                this.basePath = "~/" + basePath.TrimEnd('/', '\\');
            }
        }

        readonly string basePath;

        public string FilePattern { get; set; }

        public Regex Exclude { get; set; }

        public SearchOption SearchOption { get; set; }

        public IEnumerable<T> GetModules(IModuleFactory<T> moduleFactory, ICassetteApplication application)
        {
            var directory = GetDirectoryForBasePath(application);
            var filenames = GetFilenames(directory);

            return from filename in filenames
                   select CreateModule(filename, moduleFactory, directory);
        }

        IDirectory GetDirectoryForBasePath(ICassetteApplication application)
        {
            return basePath.Length == 1
                       ? application.RootDirectory
                       : application.RootDirectory.GetDirectory(basePath.Substring(2), false);
        }

        IEnumerable<string> GetFilenames(IDirectory directory)
        {
            IEnumerable<string> filenames;
            if (string.IsNullOrEmpty(FilePattern))
            {
                filenames = directory.GetFilePaths("", SearchOption, "*");
            }
            else
            {
                var patterns = FilePattern.Split(';', ',');
                filenames = patterns.SelectMany(
                    pattern => directory.GetFilePaths("", SearchOption, pattern)
                );
            }

            if (Exclude != null)
            {
                filenames = filenames.Where(f => Exclude.IsMatch(f) == false);
            }
            return filenames;
        }

        T CreateModule(string filename, IModuleFactory<T> moduleFactory, IDirectory directory)
        {
            var name = RemoveFileExtension(filename);
            var module = moduleFactory.CreateModule(PathUtilities.CombineWithForwardSlashes(basePath, name));
            var asset = new Asset(
                PathUtilities.CombineWithForwardSlashes(basePath, filename),
                module,
                directory.GetFile(filename)
            );
            module.AddAssets(new[] { asset }, true);
            return module;
        }

        string RemoveFileExtension(string filename)
        {
            var index = filename.LastIndexOf('.');
            return (index >= 0) ? filename.Substring(0, index) : filename;
        }
    }
}

