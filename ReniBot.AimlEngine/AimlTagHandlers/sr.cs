using Microsoft.Extensions.Logging;
using System.Xml;

namespace ReniBot.AimlEngine.AIMLTagHandlers
{
    /// <summary>
    /// The sr element is a shortcut for: 
    /// 
    /// <srai><star/></srai> 
    /// 
    /// The atomic sr does not have any content. 
    /// </summary>
    public class Sr : Utils.AIMLTagHandler
    {
        readonly ILogger _logger;
        readonly User _user;
        readonly Utils.SubQuery _query;
        readonly Request _request;
        readonly Bot _bot;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="bot">The bot involved in this request</param>
        /// <param name="user">The user making the request</param>
        /// <param name="query">The query that originated this node</param>
        /// <param name="request">The request inputted into the system</param>
        /// <param name="result">The result to be passed to the user</param>
        /// <param name="templateNode">The node to be processed</param>
        public Sr(ILogger logger,
                        Bot bot,
                        User user,
                        Utils.SubQuery query,
                        Request request,
                        XmlNode templateNode)
            : base(logger, templateNode)
        {
            _user = user;
            _query = query;
            _request = request;
            _logger = logger;
            _bot = bot;

        }

        protected override string ProcessChange()
        {
            if (TemplateNode.Name.ToLower() == "sr")
            {
                XmlNode starNode = Utils.AIMLTagHandler.GetNode("<star/>");
                Star recursiveStar = new Star(_logger,  _query, _request, starNode);
                string starContent = recursiveStar.Transform();

                XmlNode sraiNode = ReniBot.AimlEngine.Utils.AIMLTagHandler.GetNode("<srai>" + starContent + "</srai>");
                Srai sraiHandler = new Srai(_logger, _bot, _user, _request, sraiNode);
                return sraiHandler.Transform();
            }
            return string.Empty;
        }
    }
}
