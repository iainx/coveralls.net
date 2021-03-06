﻿using System.Collections.Generic;
using System.Xml.Linq;

namespace csmacnz.Coveralls
{
    public class OpenCoverParser
    {
        private readonly IFileSystem _fileSystem;
        private readonly PathProcessor _pathProcessor;

        public OpenCoverParser(IFileSystem fileSystem, PathProcessor pathProcessor)
        {
            _fileSystem = fileSystem;
            _pathProcessor = pathProcessor;
        }

        public List<CoverageFile> GenerateSourceFiles(XDocument document, bool useRelativePaths)
        {
            var files = new List<CoverageFile>();
            if (document.Root != null)
            {
                var xElement = document.Root.Element("Modules");
                if (xElement != null)
                    foreach (var module in xElement.Elements("Module"))
                    {
                        var attribute = module.Attribute("skippedDueTo");
                        if (attribute == null || string.IsNullOrEmpty(attribute.Value))
                        {
                            var filesElement = module.Element("Files");
                            if (filesElement != null)
                            {
                                foreach (var file in filesElement.Elements("File"))
                                {
                                    var fileid = file.Attribute("uid").Value;
                                    var fullPath = file.Attribute("fullPath").Value;
                                    var path = fullPath;
                                    if (useRelativePaths)
                                    {
                                        path = _pathProcessor.ConvertPath(fullPath);
                                    }
                                    path = _pathProcessor.UnixifyPath(path);
                                    var coverageBuilder = new CoverageFileBuilder(path);

                                    var classesElement = module.Element("Classes");
                                    if (classesElement != null)
                                    {
                                        foreach (var @class in classesElement.Elements("Class"))
                                        {
                                            var methods = @class.Element("Methods");
                                            if (methods != null)
                                                foreach (var method in methods.Elements("Method"))
                                                {
                                                    var sequencePointsElement = method.Element("SequencePoints");
                                                    if (sequencePointsElement != null)
                                                        foreach (var sequencePoint in sequencePointsElement.Elements("SequencePoint"))
                                                        {
                                                            var sequenceFileid = sequencePoint.Attribute("fileid").Value;
                                                            if (fileid == sequenceFileid)
                                                            {
                                                                var sourceLine = int.Parse(sequencePoint.Attribute("sl").Value);
                                                                var visitCount = int.Parse(sequencePoint.Attribute("vc").Value);

                                                                coverageBuilder.RecordCoverage(sourceLine, visitCount);
                                                            }

                                                        }
                                                }
                                        }
                                    }

                                    var readAllText = _fileSystem.TryLoadFile(fullPath);
                                    if (readAllText != null)
                                    {
                                        coverageBuilder.AddSource(readAllText);
                                    }
                                    var coverageFile = coverageBuilder.CreateFile();
                                    files.Add(coverageFile);
                                }
                            }
                        }
                    }
            }
            return files;
        }
    }
}
