using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace HellForge
{
    class Program
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger( System.Reflection.MethodBase.GetCurrentMethod( ).DeclaringType );

        static void Main( string[] args )
        {
            // Set up logging...
            log4net.Config.XmlConfigurator.Configure( );
            log.Info( "HellForge started" );

            Configuration.Initialize( );

            if ( Configuration.CurrentSettings.EvernoteUsername == Configuration.DefaultSettings.EvernoteUsername )
            {
                log.Error( "Please edit HellForge.ini to set your Evernote username and password." );
                return;
            }

            TweetMaestro.FetchAndTweet( 1 );
            
            log.Info( "Done" );
        }
    }
}
