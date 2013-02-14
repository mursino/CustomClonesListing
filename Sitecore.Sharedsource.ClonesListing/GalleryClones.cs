using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Resources;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;
using Sitecore.Shell.Applications.ContentManager.Galleries;
using Sitecore;
using Sitecore.Shell;
using System.Linq;

namespace Sitecore.Sharedsource.ClonesListing
{
    public class GalleryClones : GalleryForm
    {

        protected Scrollbox Links;

        #region Event Overrides

        /// <summary>
        /// Overrides the message handler
        /// </summary>
        /// <param name="message"></param>
        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            base.Invoke(message, true);
            message.CancelBubble = true;
            message.CancelDispatch = true;
        }
        
        /// <summary>
        /// Override the load event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (Context.ClientPage.IsEvent)
            {
                return;
            }
            StringBuilder stringBuilder = new StringBuilder();
            Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
            if (itemFromQueryString != null)
            {
                var uniqueItemAndLinkPairs = GetClones(itemFromQueryString);

                if (uniqueItemAndLinkPairs.Any())
                {
                    RenderCloneList(stringBuilder, uniqueItemAndLinkPairs);
                }
                else
                {
                    stringBuilder.Append("<div style=\"font-weight:bold;padding:2px 0px 4px 0px\">" + Translate.Text("There are no clones for this item.") + "</div>");
                }
                
            }
            
            this.Links.Controls.Add(new LiteralControl(stringBuilder.ToString()));
        }

        #endregion Event Overrides

        #region Private Helpers

        /// <summary>
        /// Renders the list of clones into the provides StringBuilder
        /// </summary>
        /// <param name="result"></param>
        /// <param name="itemAndLinkPairs"></param>
        private void RenderCloneList(StringBuilder result, List<Pair<Item, ItemLink>> itemAndLinkPairs)
        {
            result.Append("<div style=\"font-weight:bold;padding:2px 0px 4px 0px\">" + Translate.Text("Clones") + ":</div>");
            foreach (Pair<Item, ItemLink> itemToLinkPair in itemAndLinkPairs)
            {
                Item item = itemToLinkPair.Part1;
                ItemLink itemLink = itemToLinkPair.Part2;
                if (item == null)
                {
                    result.Append(string.Format("<div class=\"scLink\">{0} {1}: {2}, {3}</div>", new object[]
					{
						Images.GetImage("Applications/16x16/error.png", 16, 16, "absmiddle", "0px 4px 0px 0px"), 
						Translate.Text("Not found"), 
						itemLink.SourceDatabaseName, 
						itemLink.SourceItemID
					}));
                }
                else // item is not null
                {
                    // only show if referred via the _Source field
                    if (item.Fields.Contains(new ID("{1B86697D-60CA-4D80-83FB-7555A2E6CE1C}")))
                    {
                        result.Append(string.Concat(new object[]
					    {
						    "<a href=\"#\" class=\"scLink\" onclick='javascript:return scForm.invoke(\"item:load(id=", 
						    item.ID, 
						    ",language=", 
						    item.Language, 
						    ",version=", 
						    item.Version, 
						    ")\")'>", 
						    Images.GetImage(item.Appearance.Icon, 16, 16, "absmiddle", "0px 4px 0px 0px"), 
						    item.DisplayName
					    }));
                        result.Append(" - [").Append(item.Paths.Path).Append("]</a><br />");
                    }
                }
            }
        }

        /// <summary>
        /// Gets a list of pairs mapping a clone to its link per the provided source item
        /// </summary>
        /// <param name="originalItem"></param>
        /// <returns></returns>
        private List<Pair<Item, ItemLink>> GetClones(Item originalItem)
        {
            List<Pair<Item, ItemLink>> resultPairs = new List<Pair<Item, ItemLink>>();
            
            LinkDatabase linkDatabase = Globals.LinkDatabase;
            ItemLink[] referrerItemLinks = linkDatabase.GetReferrers(originalItem);
            
            for (int i = 0; i < referrerItemLinks.Length; i++)
            {
                ItemLink itemLink = referrerItemLinks[i];
                Database database = Factory.GetDatabase(itemLink.SourceDatabaseName, false);
                if (database != null)
                {
                    Item item = database.Items[itemLink.SourceItemID];
                    if (item == null || !this.IsHidden(item) || UserOptions.View.ShowHiddenItems && !item.TemplateName.Equals("Template field"))
                    {
                        resultPairs.Add(new Pair<Item, ItemLink>(item, itemLink));
                    }
                }
            }

            // only include references via the _Source field (aka clones)
            resultPairs = resultPairs.Where(p => p.Part1.Fields.Contains(new ID("{1B86697D-60CA-4D80-83FB-7555A2E6CE1C}"))).ToList();

            // filter out duplicate clone references due to language versions
            return resultPairs.GroupBy(p => p.Part1.ID).Select(g => g.FirstOrDefault()).ToList();
        }

        /// <summary>
        /// Determines if an item is hidden
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool IsHidden(Item item)
        {
            while (item != null)
            {
                if (item.Appearance.Hidden)
                {
                    return true;
                }
                item = item.Parent;
            }
            return false;
        }

        #endregion Private Helpers
    }
}
