using ReniBot.AimlEngine.Utils;
using ReniBot.Common;

namespace ReniBot.AimlEngine
{
    /// <summary>
    /// Encapsulates information and history of a user who has interacted with the bot
    /// </summary>
    public class User
    {
        public int AppId { get; set; }

        /// <summary>
        /// The GUID that identifies this user to the bot
        /// </summary>
        public string UserKey { get; set; }

        public int UserId { get; set; }

        /// <summary>
        /// A collection of all the result objects returned to the user in this session
        /// </summary>
        private readonly IUserResultService _results;

        /// <summary>
        /// the value of the "topic" predicate
        /// </summary>
        public string Topic
        {
            get
            {
                return _predicates.grabSetting("topic");
            }
        }

        /// <summary>
        /// the predicates associated with this particular user
        /// </summary>
        private readonly IUserPredicateService _predicates;

        public IUserPredicateService Predicates { get { return _predicates; } }

        /// <summary>
        /// The most recent result to be returned by the bot
        /// </summary>
        public Result LastResult
        {
            get
            {
                return _converter.Convert(_results.GetLast(UserId));
            }
        }

        private readonly IUserRequestService _requestService;
        private readonly BotConverter _converter;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="userKey">The GUID of the user</param>
        /// <param name="bot">the bot the user is connected to</param>
		public User(IUserPredicateService userPredicateService, IUserResultService userResultService, IUserRequestService requestService)
        {
            _predicates = userPredicateService;
            _results = userResultService;
            //bot.DefaultPredicates.Clone(Predicates);
            _requestService = requestService;
            _predicates.addSetting("topic", "*");
            _converter = new BotConverter(_requestService);
        }


        /// <summary>
        /// Returns the string to use for the next that part of a subsequent path
        /// </summary>
        /// <returns>the string to use for that</returns>
        public string GetLastBotOutput()
        {
            if (_results.Count(UserId) > 0)
            {
                return (_converter.Convert(_results.GetLast(UserId))).RawOutput;
            }
            else
            {
                return "*";
            }
        }

        /// <summary>
        /// Returns the first sentence of the last output from the bot
        /// </summary>
        /// <returns>the first sentence of the last output from the bot</returns>
        public string GetThat()
        {
            return GetThat(0, 0);
        }

        /// <summary>
        /// Returns the first sentence of the output "n" steps ago from the bot
        /// </summary>
        /// <param name="n">the number of steps back to go</param>
        /// <returns>the first sentence of the output "n" steps ago from the bot</returns>
        public string GetThat(int n)
        {
            return GetThat(n, 0);
        }

        /// <summary>
        /// Returns the sentence numbered by "sentence" of the output "n" steps ago from the bot
        /// </summary>
        /// <param name="n">the number of steps back to go</param>
        /// <param name="sentence">the sentence number to get</param>
        /// <returns>the sentence numbered by "sentence" of the output "n" steps ago from the bot</returns>
        public string GetThat(int n, int sentence)
        {
            if ((n >= 0) & (n < _results.Count(UserId)))
            {
                Result historicResult = _converter.Convert(_results.GetNResult(n, UserId));
                if ((sentence >= 0) & (sentence < historicResult.OutputSentences.Count))
                {
                    return historicResult.OutputSentences[sentence];
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Returns the first sentence of the last output from the bot
        /// </summary>
        /// <returns>the first sentence of the last output from the bot</returns>
        public string GetResultSentence()
        {
            return GetResultSentence(0, 0);
        }

        /// <summary>
        /// Returns the first sentence from the output from the bot "n" steps ago
        /// </summary>
        /// <param name="n">the number of steps back to go</param>
        /// <returns>the first sentence from the output from the bot "n" steps ago</returns>
        public string GetResultSentence(int n)
        {
            return GetResultSentence(n, 0);
        }

        /// <summary>
        /// Returns the identified sentence number from the output from the bot "n" steps ago
        /// </summary>
        /// <param name="n">the number of steps back to go</param>
        /// <param name="sentence">the sentence number to return</param>
        /// <returns>the identified sentence number from the output from the bot "n" steps ago</returns>
        public string GetResultSentence(int n, int sentence)
        {
            if ((n >= 0) & (n < _results.Count(UserId)))
            {
                Result historicResult = _converter.Convert(_results.GetNResult(n, UserId));
                if ((sentence >= 0) & (sentence < historicResult.InputSentences.Count))
                {
                    return historicResult.InputSentences[sentence];
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Adds the latest result from the bot to the Results collection
        /// </summary>
        /// <param name="latestResult">the latest result from the bot</param>
        public void AddResult(Result latestResult)
        {
            _results.Add(latestResult.Duration.Milliseconds, latestResult.HasTimedOut, latestResult.RawOutput,
                latestResult.RequestId, latestResult.UserId);
        }
    }
}