using Microsoft.Extensions.Logging;
using System;
using System.Xml;

namespace ReniBot.AimlEngine.AIMLTagHandlers
{
    /// <summary>
    /// The topicstar element tells the AIML interpreter that it should substitute the contents of 
    /// a wildcard from the current topic (if the topic contains any wildcards).
    /// 
    /// The topicstar element has an optional integer index attribute that indicates which wildcard 
    /// to use; the minimum acceptable value for the index is "1" (the first wildcard). Not 
    /// specifying the index is the same as specifying an index of "1". 
    /// 
    /// The topicstar element does not have any content. 
    /// </summary>
    public class Topicstar : Utils.AIMLTagHandler
    {
        readonly Utils.SubQuery _query;
        readonly Request _request;
        readonly ILogger _logger;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="bot">The bot involved in this request</param>
        /// <param name="user">The user making the request</param>
        /// <param name="query">The query that originated this node</param>
        /// <param name="request">The request inputted into the system</param>
        /// <param name="result">The result to be passed to the user</param>
        /// <param name="templateNode">The node to be processed</param>
        public Topicstar(ILogger logger,
                        ReniBot.AimlEngine.Utils.SubQuery query,
                        ReniBot.AimlEngine.Request request,
                        XmlNode templateNode)
            : base(logger, templateNode)
        {
            _logger = logger;
            _request = request;
            _query = query;
        }

        protected override string ProcessChange()
        {
            if (TemplateNode.Name.ToLower() == "topicstar")
            {
                if (TemplateNode.Attributes.Count == 0)
                {
                    if (_query.TopicStar.Count > 0)
                    {
                        return _query.TopicStar[0];
                    }
                    else
                    {
                        _logger.LogError("An out of bounds index to topicstar was encountered when processing the input: " + _request.RawInput);
                    }
                }
                else if (TemplateNode.Attributes.Count == 1)
                {
                    if (TemplateNode.Attributes[0].Name.ToLower() == "index")
                    {
                        if (TemplateNode.Attributes[0].Value.Length > 0)
                        {
                            try
                            {
                                int result = Convert.ToInt32(TemplateNode.Attributes[0].Value.Trim());
                                if (_query.TopicStar.Count > 0)
                                {
                                    if (result > 0)
                                    {
                                        return _query.TopicStar[result - 1];
                                    }
                                    else
                                    {
                                        _logger.LogError("An input tag with a bady formed index (" + TemplateNode.Attributes[0].Value + ") was encountered processing the input: " + _request.RawInput);
                                    }
                                }
                                else
                                {
                                    _logger.LogError("An out of bounds index to topicstar was encountered when processing the input: " + _request.RawInput);
                                }
                            }
                            catch
                            {
                                _logger.LogError("A thatstar tag with a bady formed index (" + TemplateNode.Attributes[0].Value + ") was encountered processing the input: " + _request.RawInput);
                            }
                        }
                    }
                }
            }
            return string.Empty;
        }
    }
}
