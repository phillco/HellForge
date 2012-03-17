using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evernote.EDAM.Type;

namespace HellForge
{
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
                GuidCache.Add( note.Guid ); // Mark as tweeted FIRST. Double tweeting is worse than missing a note here and there.
                TwitterPoster.Tweet( tweet, note.Resources[0].Data.Body );
                Console.WriteLine( );
            }
        }
    }
}
