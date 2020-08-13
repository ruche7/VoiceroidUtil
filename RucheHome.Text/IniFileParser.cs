using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace RucheHome.Text
{
    /// <summary>
    /// INIファイルパーサクラス。
    /// </summary>
    public static class IniFileParser
    {
        /// <summary>
        /// INIファイルの内容を IniFileSectionCollection オブジェクトへパースする。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        /// <param name="strict">厳格な形式チェックを行うならば true 。</param>
        /// <returns>IniFileSectionCollection オブジェクト。</returns>
        public static IniFileSectionCollection FromFile(string filePath, bool strict = false) =>
            (filePath == null) ?
                throw new ArgumentNullException(nameof(filePath)) :
                FromString(TextFileReader.Read(filePath), strict);

        /// <summary>
        /// INIファイルの内容を IniFileSectionCollection オブジェクトへパースする。
        /// </summary>
        /// <param name="filePath">ファイル情報。</param>
        /// <param name="strict">厳格な形式チェックを行うならば true 。</param>
        /// <returns>IniFileSectionCollection オブジェクト。</returns>
        public static IniFileSectionCollection FromFile(
            FileInfo fileInfo,
            bool strict = false)
            =>
            (fileInfo == null) ?
                throw new ArgumentNullException(nameof(fileInfo)) :
                FromString(TextFileReader.Read(fileInfo), strict);

        /// <summary>
        /// INIファイル形式文字列値を IniFileSectionCollection オブジェクトへパースする。
        /// </summary>
        /// <param name="iniString">INIファイル形式文字列値。</param>
        /// <param name="strict">厳格な形式チェックを行うならば true 。</param>
        /// <returns>IniFileSectionCollection オブジェクト。</returns>
        public static IniFileSectionCollection FromString(
            string iniString,
            bool strict = false)
        {
            if (iniString == null)
            {
                throw new ArgumentNullException(nameof(iniString));
            }

            var ini = new IniFileSectionCollection();

            foreach (var line in ReadLines(iniString))
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.Length == 0 || trimmedLine[0] == ';')
                {
                    continue;
                }

                if (trimmedLine[0] == '[')
                {
                    if (trimmedLine[trimmedLine.Length - 1] == ']')
                    {
                        AddSection(ini, trimmedLine.Substring(1, trimmedLine.Length - 2));
                    }
                }

                var eqIndex = line.IndexOf('=');
                if (eqIndex < 0)
                {
                    if (strict)
                    {
                        throw new FormatException(@"Invalid line.");
                    }
                    continue;
                }

                var name = line.Substring(0, eqIndex).Trim();
                var value = line.Substring(eqIndex + 1).TrimStart();

                if (ini.Count <= 0)
                {
                    if (strict)
                    {
                        throw new FormatException(
                            "The item (\"" + name + "\") is found before a section.");
                    }
                    continue;
                }

                AddItemToLastSection(ini, name, value);
            }

            return ini;
        }

        /// <summary>
        /// 文字列値から文字列を1行ずつ返す列挙を作成する。
        /// </summary>
        /// <param name="s">文字列値。</param>
        /// <returns>文字列を1行ずつ返す列挙。</returns>
        private static IEnumerable<string> ReadLines(string s)
        {
            using (var reader = new StringReader(s))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }
                    yield return line;
                }
            }
        }

        /// <summary>
        /// IniFileSectionCollection オブジェクトへセクションを追加する。
        /// </summary>
        /// <param name="dest">IniFileSectionCollection オブジェクト。</param>
        /// <param name="sectionName">セクション名。</param>
        private static void AddSection(IniFileSectionCollection dest, string sectionName)
        {
            Debug.Assert(dest != null);

            IniFileSection section;
            try
            {
                section = new IniFileSection(sectionName);
            }
            catch (Exception ex)
            {
                throw new FormatException(@"Invalid section name.", ex);
            }

            try
            {
                dest.Add(section);
            }
            catch (Exception ex)
            {
                throw new FormatException(
                    "The section (\"" + sectionName + "\") is duplicated.",
                    ex);
            }
        }

        /// <summary>
        /// IniFileSectionCollection オブジェクトの末尾セクションへアイテムを追加する。
        /// </summary>
        /// <param name="dest">IniFileSectionCollection オブジェクト。</param>
        /// <param name="name">名前。</param>
        /// <param name="value">値。</param>
        private static void AddItemToLastSection(
            IniFileSectionCollection dest,
            string name,
            string value)
        {
            Debug.Assert(dest != null && dest.Count > 0);

            var section = dest[dest.Count - 1];

            IniFileItem item;
            try
            {
                item = new IniFileItem(name);
            }
            catch (Exception ex)
            {
                throw new FormatException(
                    "The name of the item (in the section \"" +
                    section.Name +
                    "\") is invalid.",
                    ex);
            }

            try
            {
                item.Value = value;
            }
            catch (Exception ex)
            {
                throw new FormatException(
                    "The value of the item (\"" +
                    name +
                    "\" in the section \"" +
                    section.Name +
                    "\") is invalid.",
                    ex);
            }

            try
            {
                dest[dest.Count - 1].Items.Add(item);
            }
            catch (Exception ex)
            {
                throw new FormatException(
                    "The item (\"" +
                    name +
                    "\" in the section \"" +
                    section.Name +
                    "\") is duplicated.",
                    ex);
            }
        }
    }
}
