using Microsoft.Extensions.Logging;
using ReniBot.AimlEngine.Normalize;
using System.IO;
using System.Text;
using System.Xml;

namespace ReniBot.AimlEngine.Utils
{
    /// <summary>
    /// A utility class for loading AIML files from disk into the graphmaster structure that 
    /// forms an AIML bot's "brain"
    /// </summary>
    public class AIMLLoader : IAimlLoader
    {
        /// <summary>
        /// The bot whose brain is being processed
        /// </summary>
        //private ReniBot.AimlEngine.Bot bot;
        private readonly ILogger _logger;
        private readonly bool _trustAiml;
        private readonly int _maxThatSize;
        private readonly ApplySubstitutions _substitutor;
        private readonly StripIllegalCharacters _stripper;

        public Node GraphMaster { get; protected set; }

        public int GraphSize { get; private set; } = 0;

        public void SetGraphMaster(Node graphMaster)
        {
            GraphMaster = graphMaster;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="bot">The bot whose brain is being processed</param>
        public AIMLLoader(ILogger logger, ApplySubstitutions substitutor, StripIllegalCharacters stripper, bool trustAiml, int maxThatSize)
        {
            _logger = logger;
            GraphMaster = new Node();
            _maxThatSize = maxThatSize;
            _substitutor = substitutor;
            _stripper = stripper;
            _trustAiml = trustAiml;
        }


        /// <summary>
        /// Loads the AIML from files found in the path
        /// </summary>
        /// <param name="path"></param>
        public Node LoadAIML(string path)
        {
            if (Directory.Exists(path))
            {
                // process the AIML
                _logger.LogInformation("Starting to process AIML files found in the directory {0}", path);

                string[] fileEntries = Directory.GetFiles(path, "*.aiml");
                if (fileEntries.Length > 0)
                {
                    foreach (string filename in fileEntries)
                    {
                        loadAIMLFile(filename);
                    }
                    _logger.LogInformation("Finished processing the AIML files. ");
                }
                else
                {
                    throw new FileNotFoundException("Could not find any .aiml files in the specified directory ( {0}). Please make sure that your aiml file end in a lowercase aiml extension, for example - myFile.aiml is valid but myFile.AIML is not.", path);
                }
            }
            else
            {
                throw new FileNotFoundException("The directory specified as the path to the AIML files (" + path + ") cannot be found by the AIMLLoader object. Please make sure the directory where you think the AIML files are to be found is the same as the directory specified in the settings file.");
            }
            return GraphMaster;
        }

        /// <summary>
        /// Given the name of a file in the AIML path directory, attempts to load it into the 
        /// graphmaster
        /// </summary>
        /// <param name="filename">The name of the file to process</param>
        public Node loadAIMLFile(string filename)
        {
            _logger.LogInformation("Processing AIML file: {0}", filename);

            // load the document
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);
            loadAIMLFromXML(doc, filename);
            return GraphMaster;
        }

        /// <summary>
        /// Given an XML document containing valid AIML, attempts to load it into the graphmaster
        /// </summary>
        /// <param name="doc">The XML document containing the AIML</param>
        /// <param name="filename">Where the XML document originated</param>
        public Node loadAIMLFromXML(XmlDocument doc, string filename)
        {
            // Get a list of the nodes that are children of the <aiml> tag
            // these nodes should only be either <topic> or <category>
            // the <topic> nodes will contain more <category> nodes
            XmlNodeList rootChildren = doc.DocumentElement.ChildNodes;

            // process each of these child nodes
            foreach (XmlNode currentNode in rootChildren)
            {
                if (currentNode.Name == "topic")
                {
                    ProcessTopic(currentNode, filename);
                }
                else if (currentNode.Name == "category")
                {
                    ProcessCategory(currentNode, filename);
                }
            }
            return GraphMaster;
        }

        /// <summary>
        /// Given a "topic" node, processes all the categories for the topic and adds them to the 
        /// graphmaster "brain"
        /// </summary>
        /// <param name="node">the "topic" node</param>
        /// <param name="filename">the file from which this node is taken</param>
        private void ProcessTopic(XmlNode node, string filename)
        {
            // find the name of the topic or set to default "*"
            string topicName = "*";
            if ((node.Attributes.Count == 1) & (node.Attributes[0].Name == "name"))
            {
                topicName = node.Attributes["name"].Value;
            }

            // process all the category nodes
            foreach (XmlNode thisNode in node.ChildNodes)
            {
                if (thisNode.Name == "category")
                {
                    ProcessCategory(thisNode, topicName, filename);
                }
            }
        }

        /// <summary>
        /// Adds a category to the graphmaster structure using the default topic ("*")
        /// </summary>
        /// <param name="node">the XML node containing the category</param>
        /// <param name="filename">the file from which this category was taken</param>
        private void ProcessCategory(XmlNode node, string filename)
        {
            ProcessCategory(node, "*", filename);
        }

        /// <summary>
        /// Adds a category to the graphmaster structure using the given topic
        /// </summary>
        /// <param name="node">the XML node containing the category</param>
        /// <param name="topicName">the topic to be used</param>
        /// <param name="filename">the file from which this category was taken</param>
        private void ProcessCategory(XmlNode node, string topicName, string filename)
        {
            // reference and check the required nodes
            XmlNode pattern = FindNode("pattern", node);
            XmlNode template = FindNode("template", node);

            if (object.Equals(null, pattern))
            {
                throw new XmlException("Missing pattern tag in a node found in " + filename);
            }
            if (object.Equals(null, template))
            {
                throw new XmlException("Missing template tag in the node with pattern: " + pattern.InnerText + " found in " + filename);
            }

            string categoryPath = GeneratePath(node, topicName, false);

            // o.k., add the processed AIML to the GraphMaster structure
            if (categoryPath.Length > 0)
            {
                try
                {
                    GraphMaster.AddCategory(categoryPath, template.OuterXml, filename);
                    // keep count of the number of categories that have been processed
                    GraphSize++;
                }
                catch
                {
                    _logger.LogError("Failed to load a new category into the graphmaster where the path = " + categoryPath + " and template = " + template.OuterXml + " produced by a category in the file: " + filename);
                }
            }
            else
            {
                _logger.LogWarning("Attempted to load a new category with an empty pattern where the path = " + categoryPath + " and template = " + template.OuterXml + " produced by a category in the file: " + filename);
            }
        }

        /// <summary>
        /// Generates a path from a category XML node and topic name
        /// </summary>
        /// <param name="node">the category XML node</param>
        /// <param name="topicName">the topic</param>
        /// <param name="isUserInput">marks the path to be created as originating from user input - so
        /// normalize out the * and _ wildcards used by AIML</param>
        /// <returns>The appropriately processed path</returns>
        private string GeneratePath(XmlNode node, string topicName, bool isUserInput)
        {
            // get the nodes that we need
            XmlNode pattern = FindNode("pattern", node);
            XmlNode that = FindNode("that", node);

            string patternText;
            string thatText = "*";
            if (object.Equals(null, pattern))
            {
                patternText = string.Empty;
            }
            else
            {
                patternText = pattern.InnerText;
            }
            if (!object.Equals(null, that))
            {
                thatText = that.InnerText;
            }

            return GeneratePath(patternText, thatText, topicName, isUserInput);
        }

        /// <summary>
        /// Given a name will try to find a node named "name" in the childnodes or return null
        /// </summary>
        /// <param name="name">The name of the node</param>
        /// <param name="node">The node whose children need searching</param>
        /// <returns>The node (or null)</returns>
        private XmlNode FindNode(string name, XmlNode node)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == name)
                {
                    return child;
                }
            }
            return null;
        }

        /// <summary>
        /// Generates a path from the passed arguments
        /// </summary>
        /// <param name="pattern">the pattern</param>
        /// <param name="that">the that</param>
        /// <param name="topicName">the topic</param>
        /// <param name="isUserInput">marks the path to be created as originating from user input - so
        /// normalize out the * and _ wildcards used by AIML</param>
        /// <returns>The appropriately processed path</returns>
        public string GeneratePath(string pattern, string that, string topicName, bool isUserInput)
        {
            // to hold the normalized path to be entered into the graphmaster
            StringBuilder normalizedPath = new StringBuilder();
            string normalizedTopic;
            string normalizedThat;
            string normalizedPattern;
            if ((_trustAiml) & (!isUserInput))
            {
                normalizedPattern = pattern.Trim();
                normalizedThat = that.Trim();
                normalizedTopic = topicName.Trim();
            }
            else
            {
                normalizedPattern = Normalize(pattern, isUserInput).Trim();
                normalizedThat = Normalize(that, isUserInput).Trim();
                normalizedTopic = Normalize(topicName, isUserInput).Trim();
            }

            // check sizes
            if (normalizedPattern.Length > 0)
            {
                if (normalizedThat.Length == 0)
                {
                    normalizedThat = "*";
                }
                if (normalizedTopic.Length == 0)
                {
                    normalizedTopic = "*";
                }

                // This check is in place to avoid huge "that" elements having to be processed by the 
                // graphmaster. 
                if (normalizedThat.Length > _maxThatSize)
                {
                    normalizedThat = "*";
                }

                // o.k. build the path
                normalizedPath.Append(normalizedPattern);
                normalizedPath.Append(" <that> ");
                normalizedPath.Append(normalizedThat);
                normalizedPath.Append(" <topic> ");
                normalizedPath.Append(normalizedTopic);

                return normalizedPath.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Given an input, provide a normalized output
        /// </summary>
        /// <param name="input">The string to be normalized</param>
        /// <param name="isUserInput">True if the string being normalized is part of the user input path - 
        /// flags that we need to normalize out * and _ chars</param>
        /// <returns>The normalized string</returns>
        private string Normalize(string input, bool isUserInput)
        {
            StringBuilder result = new StringBuilder();

            string substitutedInput = _substitutor.Transform(input);
            // split the pattern into it's component words
            string[] substitutedWords = substitutedInput.Split(" \r\n\t".ToCharArray());

            // Normalize all words unless they're the AIML wildcards "*" and "_" during AIML loading
            foreach (string word in substitutedWords)
            {
                string normalizedWord;
                if (isUserInput)
                {
                    normalizedWord = _stripper.Transform(word);
                }
                else
                {
                    if ((word == "*") || (word == "_"))
                    {
                        normalizedWord = word;
                    }
                    else
                    {
                        normalizedWord = _stripper.Transform(word);
                    }
                }
                result.Append(normalizedWord.Trim() + " ");
            }

            return result.ToString().Replace("  ", " "); // make sure the whitespace is neat
        }
    }
}
