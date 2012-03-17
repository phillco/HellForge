using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HellForge
{
    /// <summary>
    /// Stores a cache of the GUIDs that we've already tweeted, so that they are not repeated.
    /// </summary>
    class GuidCache
    {
        /// <summary>Returns a thread-safe copy of the GUID list</summary>
        public static List<String> Guids
        {
            get
            {
                if ( !loaded )
                    Read( );

                lock ( _guids )
                    return new List<string>( _guids );
            }
        }

        /// <summary>The internal list</summary>
        private static List<String> _guids = new List<string>();

        /// <summary>Whether the list was loaded from the file yet</summary>
        private static bool loaded = false;

        /// <summary>
        /// Adds the given GUID to the cache.
        /// </summary>
        public static void Add( string guid )
        {
            lock ( _guids )
            {
                _guids.Add( guid );
                Write( );
            }
        }

        /// <summary>
        /// Removes the given GUID from the cache.
        /// </summary>
        public static void Remove( string guid )
        {
            lock ( _guids )
            {                
                _guids.Remove( guid );
                Write( );
            }
        }


        /// <summary>
        /// Returns whether the note with the given GUID has been tweeted.
        /// </summary>
        public static bool Contains( string guid )
        {
            return Guids.Contains( guid );
        }

        /// <summary>
        /// Reads the list of GUIDs from a file.
        /// </summary>
        private static void Read( )
        {
            lock ( _guids )
            {
                _guids.Clear( );

                try
                {
                    using ( StreamReader input = new StreamReader( "TweetedGuids.cache" ) )
                    {
                        while ( !input.EndOfStream )
                            _guids.Add( input.ReadLine( ) );
                    }
                }
                catch ( FileNotFoundException ) { } // No sweat

                loaded = true;
            }
        }

        /// <summary>
        /// Writes the list of GUIDs to a file.
        /// </summary>
        private static void Write( )
        {
            lock ( _guids )
            {
                using ( StreamWriter output = new StreamWriter( "TweetedGuids.cache", false ) )
                {
                    foreach ( string guid in _guids )
                        output.WriteLine( guid );
                }
            }
        }
    }
}
