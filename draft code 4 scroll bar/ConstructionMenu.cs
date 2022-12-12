﻿using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using System;

namespace RoadsOfTheRim
{
    /*
    Nice looking cartridge-style option picker 
    Layout : 1 vertical cartridge per type of buidalble road
    Each cartridge has :
    - A square image at the top, representing the type of road
    - The name of the road below
    - A list of costs, one per line :
    > Work
    > WoodLog
    > Stone
    > Steel
    > Chemfuel
    > etc..   
    Each line starts with the icon of the resource (work uses the construction site icon)
    Upon clicking outside, the cartridge is closed with no further actions
    Upon clicking on a road icon, we set the roaddef of the site to that road and start targeting the map to add legs

    Check, among others :
    * Widgets many methods
    */

    public class ConstructionMenu : Window
    {
        private readonly RoadConstructionSite site;
        private readonly Caravan caravan;
        private readonly List<RoadDef> buildableRoads;
        public override Vector2 InitialSize => new Vector2(1700+128, 544+128);
        // trying to make the whole menu larger by making the x value above larger, original is 676+128, assuming 64 for legend, that's about 122.4 per road option- Vamp
        // 1469 is not quite large enough, but I was right, the x value above does change the whole menu width.  Let's try 1700.  Yes, that works! -Vamp
        // TO DO : Use the below to dynamically draw the window based on number of buildable roads (which could include technolcogy limits)
        // public bool resizeable = true ;
        // private bool resizeLater = true ;
        // private Rect resizeLaterRect ;

        // adding new code for scroll bar. -Vamp 12122022
        private static Vector2 scrollPosition;
        var borderRect = ConstructionMenu.InitialSize;  // I am having trouble setting up the variable borderRect and scrollContentRect.
        var scrollContentRect = borderRect;
        scrollContentRect.height -= 20;  // This should be the height of the horizontal scroll bar.
        scrollContentRect.width = 1700+128; // I'm also not sure if what I'm setting up here is correct.  I think* that the Construction Menu size is what is defined in line 37.
        scrollContentRect.x = 0;
        scrollContentRect.y = 0;
        var scrollListing = new Listing_Standard();
        Widgets.BeginScrollView(borderRect, ref scrollPosition, scrollContentRect);
        //scrollListing.Begin(scrollContentRect);  // not actually sure I need list line of code since I am not coding for a list that changes.
        Widgets.EndScrollView();

            

        public ConstructionMenu(RoadConstructionSite site , Caravan caravan)
        {
            this.site = site ;
            this.caravan = caravan ;
            buildableRoads = new List<RoadDef>() ;
            // TO DO : COunt number of buildable roads, set the resize later rect based on that
            
        }

        public int CountBuildableRoads()
        {
            foreach (RoadDef thisDef in DefDatabase<RoadDef>.AllDefs)
            {
                // Only add RoadDefs that are buildable, based on DefModExtension_RotR_RoadDef.built
                if (thisDef.HasModExtension<DefModExtension_RotR_RoadDef>() && thisDef.GetModExtension<DefModExtension_RotR_RoadDef>().built)
                {
                    buildableRoads.Add(thisDef);
                }
            }
            return (buildableRoads!=null ? buildableRoads.Count : 0);
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (Event.current.isKey && site!=null)
            {
                RoadsOfTheRim.DeleteConstructionSite(site.Tile);
                Close();
            }

            //Resources icons
            for (int i = 0; i < 9; i++)
            {
                // Icon
                Rect ResourceRect = new Rect(0, 202f + i * 40f, 32f, 32f);
                ThingDef theDef;
                switch (i)
                {
                    case 0:
                        theDef = ThingDefOf.Spark;
                        break;
                    case 1:
                        theDef = ThingDefOf.WoodLog;
                        break;
                    case 2:
                        theDef = ThingDefOf.BlocksGranite;
                        break;
                    case 3:
                        theDef = ThingDefOf.Steel;
                        break;
                    case 4:
                        theDef = ThingDefOf.Chemfuel;
                        break;
                    case 5:
                        theDef = ThingDefOf.Plasteel;
                        break;
                    case 6:
                        theDef = ThingDefOf.Uranium;
                        break;
                    case 7:
                        theDef = ThingDefOf.ComponentIndustrial;
                        break;
                    default:
                        theDef = ThingDefOf.ComponentSpacer;
                        break;
                }
                if (i==0)
                {
                    Widgets.ButtonImage(ResourceRect, ContentFinder<Texture2D>.Get("UI/Commands/AddConstructionSite"));
                }
                else
                {
                    Widgets.ThingIcon(ResourceRect, theDef);
                }
            }

            // Sections : one per type of buildable road // 144 is width, 512+128 is height - Vamp
            int nbOfSections = 0;
            Vector2 groupSize = new Vector2(144 , 512+128);
            foreach (RoadDef aDef in buildableRoads)
            {
                DefModExtension_RotR_RoadDef roadDefExtension = aDef.GetModExtension<DefModExtension_RotR_RoadDef>() ;

                // Check if a tech is necessary to build this road, don't display the road if it isn't researched yet
                ResearchProjectDef NeededTech = roadDefExtension.techNeededToBuild ;
                bool TechResearched = false ;
                if (NeededTech != null)
                {
                    TechResearched = NeededTech.IsFinished;
                }
                else 
                {
                    TechResearched = true;
                }

                // the 32f shifts the whole "table" down, 64 is the legend width, 144 is the width of each option, all these together are
                // the size of the table within the menu; none change the size of the menu however- Vamp
                if (TechResearched)
                {
                    GUI.BeginGroup(new Rect(new Vector2(64 + 144 * nbOfSections, 32f), groupSize));

                    // Buildable Road icon
                    Texture2D theButton = ContentFinder<Texture2D>.Get("UI/Commands/Build_" + aDef.defName, true);
                    Rect ButtonRect = new Rect(8, 8, 128, 128);
                    if (Widgets.ButtonImage(ButtonRect, theButton))
                    {
                        if (Event.current.button == 0)
                        {
                            SoundStarter.PlayOneShotOnCamera(SoundDefOf.Tick_High, null);
                            site.roadDef = aDef;
                            Close();
                            RoadsOfTheRim.RoadBuildingState.CurrentlyTargeting = site;
                            RoadsOfTheRim.RoadBuildingState.Caravan = caravan;
                            RoadConstructionLeg.Target(site);
                        }
                    }

                    // Buildable Road label
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.Font = GameFont.Medium;
                    Rect NameRect = new Rect(0, 144, 144f, 32f);
                    Widgets.Label(NameRect, aDef.label);

                    // Resources amounts
                    Text.Font = GameFont.Small;
                    int i = 0;
                    foreach (string resourceName in DefModExtension_RotR_RoadDef.allResourcesAndWork)
                    {
                        Rect ResourceAmountRect = new Rect(0, 176f + i++ * 40f, 144f, 32f);
                        Widgets.Label(ResourceAmountRect,
                            (roadDefExtension.GetCost(resourceName) > 0) ? (roadDefExtension.GetCost(resourceName) * ((float)RoadsOfTheRim.settings.BaseEffort / 10)).ToString() : "-"
                        );
                    }
                    GUI.EndGroup();
                    nbOfSections++;
                }
            }
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public override void PostClose() // If the menu was somehow closed without picking a road, delete the construction site
        {
            try
            {
                if (site.roadDef == null)
                {
                    RoadsOfTheRim.DeleteConstructionSite(site.Tile);
                }
            }
            catch (Exception e)
            {
                RoadsOfTheRim.DebugLog(null, e);
            }
        }
    }
}
