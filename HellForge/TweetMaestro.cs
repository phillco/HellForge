using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evernote.EDAM.Type;

namespace HellForge
{
    /// <summary>
    /// Conducts everything.
    /// </summary>
    class TweetMaestro
    {
        /// <summary>
        /// Fetches the number of specified notes from Evernote and tweets them.
        /// </summary>
        public static void FetchAndTweet( int num )
        {
            List<Note> notes = EvernoteApi.GetNotesToTweet( num );

            foreach ( Note note in notes )
                TweetNote( note );
        }

        /// <summary>
        /// Tweets the given note.
        /// </summary>
        private static void TweetNote( Note note )
        {
            string tweet = note.Title.Replace( '@', '-' ); // Temporary; don't want to callout people during testingv.         

            Console.WriteLine( "Tweeting \"" + tweet + "\"..." );
            if ( note.Resources != null && note.Resources.Count > 0 )
            {
                // Mark this note as tweeted. We do this first because double tweeting is worse than missing a note here and there.
                GuidCache.Add( note.Guid );

                // Tweet. If it failed, then mark it as untweeted.
                if ( !TwitterPoster.Tweet( tweet, note.Resources[0].Data.Body ) )
                    GuidCache.Remove( note.Guid );

                Console.WriteLine( );
            }
        }
    }
}
