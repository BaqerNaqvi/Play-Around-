
namespace MvcSignal.Models
{
    /// <summary>
    /// Player Entity
    /// </summary>
    public class User
    {
        /// <summary>
        /// User Name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// System Generated Token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Is User Turn
        /// </summary>
        public bool IsUserTurn { get; set; }

        /// <summary>
        /// Group Id
        /// </summary>
        public string GroupToken { get; set; }
    }
}