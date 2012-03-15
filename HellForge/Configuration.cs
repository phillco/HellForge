using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nini;
using Nini.Config;
using System.IO;

namespace HellForge
{
    /// <summary>
    /// Stores HellForge's user-controlled settings, which is written to an INI file.
    /// </summary>
    class Configuration
    {
        //=======================================================
        //
        // The settings themselves.
        //
        //=======================================================

        public string EvernoteUsername { get; set; }

        public string EvernotePassword { get; set; }

        //=======================================================
        //
        // Static variables & events.
        //
        //=======================================================

        /// <summary>
        /// Where the configuration file is stored.
        /// </summary>
        public const string ConfigFileName = "HellForge.ini";

        /// <summary>
        /// The latest version of the config file format. Used to detect and update older configurations when we rearrange things.
        /// </summary>
        public const int ConfigFileVersion = 1;

        /// <summary>
        /// The current, active settings.
        /// </summary>
        public static Configuration CurrentSettings { get; private set; }

        /// <summary>
        /// The factory-default settings.
        /// </summary>
        public static Configuration DefaultSettings
        {
            get
            {
                return new Configuration
                {
                    EvernoteUsername = "username-here",
                    EvernotePassword = "password-here",
                };
            }
        }

        /// <summary>
        /// The last set of settings that we saved - used for diffing.
        /// </summary>
        public static Configuration LastSavedSettings { get; private set; }

        /// <summary>
        /// Called whenever the configuration is saved.
        /// </summary>
        public static event ChangeHandler Changed;

        public delegate void ChangeHandler( Configuration oldVersion );

        /// <summary>
        /// Initializes the configuration module and loads the user's settings.
        /// </summary>
        public static void Initialize( )
        {
            CurrentSettings = LastSavedSettings = DefaultSettings;
            if ( File.Exists( ConfigFileName ) )
                Load( ConfigFileName );
            CurrentSettings.Save( );
        }

        /// <summary>
        /// Loads the current configuration from the given file.
        /// </summary>
        /// <param name="filename"></param>
        public static void Load( string filename )
        {
            IConfigSource source = new IniConfigSource( filename );

            try
            {
                int configVersion = source.Configs["General"].GetInt( "ConfigVersion", ConfigFileVersion );
                CurrentSettings.EvernoteUsername = source.Configs["General"].Get( "EvernoteUsername", DefaultSettings.EvernoteUsername );
                CurrentSettings.EvernotePassword = source.Configs["General"].Get( "EvernotePassword", DefaultSettings.EvernotePassword );

                CurrentSettings.Repair( );
            }
            catch ( ArgumentException )
            {
                CurrentSettings = DefaultSettings;
                Console.WriteLine( "There was an error reading the configuration file. The default settings have been loaded." );
            }
        }

        /// <summary>
        /// Corrects any inconsistencies in this configuration.
        /// </summary>
        public void Repair( )
        {            
        }

        /// <summary>
        /// Returns a deep copy of this configuration.
        /// </summary>
        public Configuration Clone( )
        {
            return new Configuration
            {
                EvernoteUsername = EvernoteUsername,
                EvernotePassword = EvernotePassword,
            };
        }

        /// <summary>
        /// Saves the current configuration to disk.
        /// </summary>
        public void Save( )
        {
            Repair( );

            IniConfigSource source = new IniConfigSource( );

            IConfig config = source.AddConfig( "General" );
            config.Set( "EvernoteUsername", EvernoteUsername );
            config.Set( "EvernotePassword", EvernotePassword );          
            source.Save( ConfigFileName );
            
            if ( Changed != null )
                Changed( LastSavedSettings );
            
            LastSavedSettings = Clone();
        }

        /// <summary>
        /// Loads the factory-default settings and saves them.
        /// </summary>
        public static void ResetToDefaultSettings( )
        {
            CurrentSettings = DefaultSettings;
            CurrentSettings.Save( );
        }
    }
}
