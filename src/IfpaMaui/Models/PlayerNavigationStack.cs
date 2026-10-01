using System.Collections.Generic;

namespace Ifpa.Models
{
    public static class PlayerNavigationStack
    {
        /// <summary>
        /// Given the player shown by each page in a navigation stack (bottom to top, null for pages
        /// that aren't player details), returns how many pages to pop to get back to the topmost
        /// page already showing <paramref name="playerId"/>, or null if that player isn't in the stack.
        /// </summary>
        public static int? PagesToPopTo(IReadOnlyList<int?> stackPlayerIds, int playerId)
        {
            for (int i = stackPlayerIds.Count - 1; i >= 0; i--)
            {
                if (stackPlayerIds[i] == playerId)
                    return stackPlayerIds.Count - 1 - i;
            }

            return null;
        }
    }
}
