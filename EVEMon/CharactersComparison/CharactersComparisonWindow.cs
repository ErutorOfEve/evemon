﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Controls;
using EVEMon.Common.Data;

namespace EVEMon.CharactersComparison
{
    public partial class CharactersComparisonWindow : EVEMonForm
    {
        private readonly List<Character> m_selectedCharacters = new List<Character>();
        private Timer m_tmrSelect;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CharactersComparisonWindow"/> class.
        /// </summary>
        public CharactersComparisonWindow()
        {
            InitializeComponent();
            RememberPositionKey = "CharactersComparisonWindow";
        }

        #endregion


        #region Update Methods

        /// <summary>
        /// Updates the character list.
        /// </summary>
        private void UpdateCharacterList()
        {
            lvCharacterList.BeginUpdate();
            try
            {
                IEnumerable<Character> characters = cbFilter.SelectedIndex == 0
                                                        ? EveMonClient.Characters.OrderBy(x => x.Name)
                                                        : EveMonClient.MonitoredCharacters.OrderBy(x => x.Name);
                lvCharacterList.Items.Clear();
                lvCharacterList.Items.AddRange(characters.Select(
                    character => new ListViewItem(character.Name) { Tag = character }).ToArray());
            }
            finally
            {
                lvCharacterList.EndUpdate();
            }
        }

        /// <summary>
        /// Updates the selected items.
        /// </summary>
        /// <param name="reset">if set to <c>true</c> [reset].</param>
        private void UpdateSelectedItems(bool reset = false)
        {
            if (reset)
            {
                m_selectedCharacters.Clear();
            }
            else
            {
                IEnumerable<Character> selectedItems = lvCharacterList.SelectedItems.Cast<ListViewItem>().Select(
                    x => x.Tag).OfType<Character>();

                // Add selected character
                foreach (Character character in selectedItems.Where(character => !m_selectedCharacters.Contains(character)))
                {
                    m_selectedCharacters.Add(character);
                }

                // Remove non selected character
                List<Character> selectedCharacters = new List<Character>(m_selectedCharacters);
                foreach (Character character in selectedCharacters.Where(character => !selectedItems.Contains(character)))
                {
                    m_selectedCharacters.Remove(character);
                }
            }

            UpdateContent();
        }

        /// <summary>
        /// Updates the content.
        /// </summary>
        private void UpdateContent()
        {
            if (!m_selectedCharacters.Any())
            {
                // Hide skills groupbox
                gbAttributes.Visible = false;

                // View help message
                lblHelp.Visible = true;

                return;
            }

            UpdateCharacterInfo();

            lblHelp.Visible = false;
            gbAttributes.Visible = true;
        }

        /// <summary>
        /// Updates the character info.
        /// </summary>
        private void UpdateCharacterInfo()
        {
            lvCharacterInfo.BeginUpdate();
            try
            {
                // Refresh columns
                lvCharacterInfo.Columns.Clear();
                lvCharacterInfo.Columns.Add("Attribute");

                foreach (Character character in m_selectedCharacters)
                {
                    lvCharacterInfo.Columns.Add(character.Name, -2, HorizontalAlignment.Right);
                }

                // Prepare properties list
                List<ListViewItem> items = AddGroups();

                // Fetch the new items to the list view
                lvCharacterInfo.Items.Clear();
                lvCharacterInfo.Items.AddRange(items.ToArray());

                if (lvCharacterInfo.Items.Count > 0)
                    AdjustColumns();
            }
            finally
            {
                lvCharacterInfo.EndUpdate();
            }
        }

        /// <summary>
        /// Adds the groups.
        /// </summary>
        /// <returns></returns>
        private List<ListViewItem> AddGroups()
        {
            lvCharacterInfo.Groups.Clear();
            List<ListViewItem> items = new List<ListViewItem>();
            foreach (StaticSkillGroup skillGroup in StaticSkills.AllGroups)
            {
                ListViewGroup group = new ListViewGroup(skillGroup.Name);
                foreach (StaticSkill skill in skillGroup.Where(skill => skill.IsPublic))
                {
                    // Create the list view item
                    ListViewItem item = new ListViewItem(group) { ToolTipText = skill.Description, Text = skill.Name };
                    items.Add(item);

                    string[] labels = m_selectedCharacters.Select(
                        character => skill.ToCharacter(character).Level.ToString()).ToArray();
                    int[] values = m_selectedCharacters.Select(character => skill.ToCharacter(character).Level).ToArray();

                    // Retrieve the data to put in the columns
                    AddValueForSelectedCharacters(item, labels, values);
                }

                lvCharacterInfo.Groups.Add(group);
            }

            // Add additional info 
            AddAdditionalInfo(items);

            return items;
        }

        /// <summary>
        /// Adds the additional info.
        /// </summary>
        /// <param name="items">The items.</param>
        private void AddAdditionalInfo(ICollection<ListViewItem> items)
        {
            ListViewGroup group = new ListViewGroup("Miscellaneous");
            List<string[]> additionalInfo = new List<string[]>
                                                {
                                                    new[] { "Total SP", "The total skillpoints of the character." },
                                                    new[] { "Known Skills", "The number of known skills of the character." },
                                                    new[]
                                                        {
                                                            "Skill Books Base Cost",
                                                            "The skill books base cost for the owned or known skills of the character."
                                                        }
                                                };

            foreach (string[] text in additionalInfo)
            {
                ListViewItem item = new ListViewItem(group) { Text = text.First(), ToolTipText = text.Last() };

                string[] labels = m_selectedCharacters.Select(
                    character => GetValue(character, text.First()).ToString("N0", CultureConstants.DefaultCulture)).ToArray();
                long[] values = m_selectedCharacters.Select(character => GetValue(character, text.First())).ToArray();

                AddValueForSelectedCharacters(item, labels, values);
                items.Add(item);
            }

            lvCharacterInfo.Groups.Add(group);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        private static long GetValue(Character character, string text)
        {
            switch (text)
            {
                case "Total SP":
                    return character.SkillPoints;
                case "Known Skills":
                    return character.KnownSkillCount;
                case "Skill Books Base Cost":
                    return character.Skills.Where(skill => skill.IsKnown || skill.IsOwned).Sum(skill => skill.Cost);
                default:
                    return default(long);
            }
        }

        private void AddValueForSelectedCharacters<T>(ListViewItem item, IList<string> labels, IList<T> values)
        {
            T min = values.Any() ? values.Min() : default(T);
            T max = values.Any() ? values.Max() : default(T);
            bool allEqual = !values.Any() || values.All(value => value.Equals(min));

            // Add the value for every selected item
            for (int index = 0; index < m_selectedCharacters.Count(); index++)
            {
                // Create the subitem and choose its forecolor
                ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem(item, labels[index]);
                if (!allEqual)
                {
                    if (values[index].Equals(max))
                        subItem.ForeColor = Color.DarkGreen;
                    else if (values[index].Equals(min))
                        subItem.ForeColor = Color.DarkRed;

                    item.UseItemStyleForSubItems = false;
                }
                else if (m_selectedCharacters.Count() > 1)
                {
                    subItem.ForeColor = Color.DarkGray;
                    item.UseItemStyleForSubItems = false;
                }

                item.SubItems.Add(subItem);
            }
        }

        /// <summary>
        /// Adjusts the columns.
        /// </summary>
        private void AdjustColumns()
        {
            foreach (ColumnHeader column in lvCharacterInfo.Columns.Cast<ColumnHeader>())
            {
                column.Width = -2;

                // Due to .NET design we need to prevent the last colummn to resize to the right end

                // Return if it's not the last column
                if (column.Index != lvCharacterInfo.Columns.Count - 1)
                    continue;

                const int Pad = 4;

                // Calculate column header text width with padding
                int columnHeaderWidth = TextRenderer.MeasureText(column.Text, Font).Width + Pad * 2;

                // Calculate the width of the header and the items of the column
                int columnMaxWidth = lvCharacterInfo.Columns[column.Index].ListView.Items.Cast<ListViewItem>().Select(
                    item => TextRenderer.MeasureText(item.SubItems[column.Index].Text, Font).Width).Concat(
                        new[] { columnHeaderWidth }).Max() + Pad + 1;

                // Assign the width found
                column.Width = columnMaxWidth;
            }
        }


        #endregion


        #region Local Event Handlers

        /// <summary>
        /// Handles the Load event of the CharacterComparisonWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void CharacterComparisonWindow_Load(object sender, EventArgs e)
        {
            if (DesignMode || this.IsDesignModeHosted())
                return;

            m_tmrSelect = new Timer();
            m_tmrSelect.Tick += tmrSelect_Tick;

            ListViewHelper.EnableDoubleBuffer(lvCharacterList);
            ListViewHelper.EnableDoubleBuffer(lvCharacterInfo);

            //cbFilter.SelectedIndex = Settings.CharacterComparison.Filter;
            cbFilter.SelectedIndex = 0;
            chCharacters.Width = lvCharacterList.ClientSize.Width;

            UpdateCharacterList();
            UpdateSelectedItems(true);
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cbFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void cbFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Settings.CharacterComparison.Filter = cbFilter.SelectedIndex;
            UpdateCharacterList();
            UpdateSelectedItems(true);
        }

        /// <summary>
        /// Handles the SplitterMoved event of the persistentSplitContainer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.SplitterEventArgs"/> instance containing the event data.</param>
        private void persistentSplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
        {
            chCharacters.Width = lvCharacterList.ClientSize.Width;
        }

        /// <summary>
        /// Handles the Resize event of the persistentSplitContainer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void persistentSplitContainer_Resize(object sender, EventArgs e)
        {
            chCharacters.Width = lvCharacterList.ClientSize.Width;
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the lvCharacterList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void lvCharacterList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_tmrSelect.Enabled)
                return;

            m_tmrSelect.Start();
        }

        /// <summary>
        /// When the selection update timer ticks, we process the changes caused by a selection change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tmrSelect_Tick(object sender, EventArgs e)
        {
            m_tmrSelect.Stop();

            if (lvCharacterList.SelectedIndices.Count == 0)
                UpdateSelectedItems(true);
            else
                UpdateSelectedItems();
        }

        #endregion

    }
}