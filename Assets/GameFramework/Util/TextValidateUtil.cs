using hyjiacan.py4n;
using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = GameFramework.Debug;
using Newtonsoft.Json;

/// <summary>
/// 文本校验工具
/// </summary>
public static class TextValidateUtil
{
    /// <summary>
    /// 检查重复发言的时间间隔，单位毫秒
    /// </summary>
    public const int RepeatMessageTimeRange = 30 * 60 * 1000;
    /// <summary>
    /// 过滤规则
    /// </summary>
    public class ValidateRule
    {
        /// <summary>
        /// 文本相似度阈值，两个文本相似度检测后大于等于这个值将被判定为重复文本
        /// </summary>
        public double similarityThreshold;
        /// <summary>
        /// 单条过滤规则列表
        /// </summary>
        public List<SingleRule> singleRule;
        /// <summary>
        /// 组合过滤规则列表
        /// </summary>
        public List<CombineRule> combineRule;
    }

    /// <summary>
    /// 组合过滤规则，则多条单个过滤规则组成
    /// 单条规则匹配成功数量大于等于minMatchCount认为这条组合规则匹配成功
    /// </summary>
    public class CombineRule
    {
        public int minMatchCount;

        public List<SingleRule> rules;
    }

    /// <summary>
    /// 单条过滤规则，可指定使用拼音，正则进行匹配，注意，拼音与正则可以叠加使用哦
    /// 开启拼音匹配会将输入文本进行拼音化（去除声调）后，再进行匹配
    /// 开启正则将会把规则文本当作正则规则来对输入文本进行匹配
    /// </summary>
    public class SingleRule
    {
        public bool matchPinYin;
        public bool useRegex;
        public List<string> wordList;
        public string matchedWord;

        public bool Match(string text)
        {
            foreach (string word in wordList)
            {
                if (useRegex)
                {
                    Regex regex = new Regex(word);
                    if (regex.IsMatch(text))
                    {
                        matchedWord = word;
                        return true;
                    }
                }
                else
                {
                    if (text.Contains(word))
                    {
                        matchedWord = word;
                        return true;
                    }
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 将敏感词的每个字符拆开后创建的N维树，可以极大的提升匹配效率
    /// 因为敏感词数量极多，数量级已经达到10W级，如果使用传统的逐行匹配，将引起明显的游戏卡顿
    /// </summary>
    public class NTree
    {
        public Dictionary<char, NTree> children = new Dictionary<char, NTree>();

        public char c;
        public int index;
        public bool isEnd;
    }

    /// <summary>
    /// 匹配结果的缓存，匹配前先查询是否有缓存过，可以进一步提升匹配的效率，避免重复匹配同一文本
    /// </summary>
    private static Dictionary<string, ValueTuple<string, bool>> TextFileterdCache = new Dictionary<string, ValueTuple<string, bool>>(1000);
    /// <summary>
    /// N维树实例
    /// </summary>
    private static Dictionary<char, NTree> wordBlockTree;

    /// <summary>
    /// 半小时内的聊天信息，Key是聊天文本经过提纯处理（移除所有非中文字符）后的文本，Value是文本时间戳
    /// </summary>
    private static Dictionary<string, long> chatLimitInfoDict;
    /// <summary>
    /// 半小时内的聊天信息的客户端缓存文件
    /// </summary>
    private static string chatHistoryFile;

    /// <summary>
    /// 文本提纯处理正则（移除所有非常用汉字字符，Unicode编码在[龥]字后面的都算生僻字，统统过滤掉）
    /// </summary>
    private static Regex removeNotChineseCharacterRegex = new Regex(@"[^一-龥]");
    /// <summary>
    /// 换行符正则（匹配所有可以导致换行的字符）
    /// </summary>
    private static Regex newLineRegex = new Regex(@"[\f\n\r\t\v\x85]");

    /// <summary>
    /// 过滤规则实例
    /// </summary>
    private static ValidateRule filterRule;

    public static void Init(string filterRuleURL, string[] blockWordList)
    {
        //从文件中读出上次的聊天历史
        chatHistoryFile = Path.Combine(Application.persistentDataPath, "chatHistory.dat");
        if (File.Exists(chatHistoryFile))
        {
            string[] chatHistory = File.ReadAllLines(chatHistoryFile);
            List<string> newHistory = new List<string>(chatHistory.Length);
            chatLimitInfoDict = new Dictionary<string, long>(chatHistory.Length * 2);
            long timestampNow = TimerSystem.TimestampMillisecond;
            //相同聊天信息不能重复发送的时间限制：RepeatMessageTimeRange
            foreach (string history in chatHistory)
            {
                string[] splits = history.Split(',');
                if (splits.Length > 1)
                {
                    string msg = splits[0];
                    long timestamp = long.Parse(splits[1]);
                    if (Mathf.Abs(timestampNow - timestamp) < RepeatMessageTimeRange)
                    {
                        chatLimitInfoDict.Add(msg, timestamp);
                        newHistory.Add(history);
                    }
                }
            }
            //更新一下文件，把过期的信息删除
            File.WriteAllLines(chatHistoryFile, newHistory.ToArray());
        }
        else
        {
            chatLimitInfoDict = new Dictionary<string, long>(100);
        }
        BuildWorldBlockTree(blockWordList);

        if (string.IsNullOrEmpty(filterRuleURL) == false)
        {
            DownloadMessageFilterRule(filterRuleURL);
        }
    }

    private static void DownloadMessageFilterRule(string filterRuleURL)
    {
        WebRequestUtil.Get(filterRuleURL, null, DownloadMessageFilterRuleCallback);
    }

    private static void DownloadMessageFilterRuleCallback(ResponseData webRequest)
    {
        if (webRequest.succeed)
        {
            Debug.Log($"下载文本过滤规则成功；消息内容：{webRequest.text}", "聊天过滤");
            filterRule = JsonConvert.DeserializeObject<ValidateRule>(webRequest.text);
        }
        else
        {
            Debug.LogError($"下载文本过滤规则失败：错误：{webRequest.text}", "聊天过滤");
        }
    }

    /// <summary>
    /// 记录聊天消息，用于重复文本判定，要在聊天消息发送成功后调用
    /// </summary>
    /// <param name="msg"></param>
    public static void RecordChatMessage(this string msg)
    {
        string msgPure = removeNotChineseCharacterRegex.Replace(msg, string.Empty);
        chatLimitInfoDict.Add(msgPure, TimerSystem.TimestampMillisecond);
        File.AppendAllText(chatHistoryFile, string.Format("{0},{1}\n", msgPure, TimerSystem.TimestampMillisecond));
    }

    /// <summary>
    /// 判断输入文本是否为合法聊天消息（不包含任何换行符，重复，广告）
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static bool IsValidChatMessage(this string msg)
    {
        bool valid = !msg.IsNewLineMessage();

        if (valid)
        {
            valid = !msg.IsRepeatMessage();
        }

        if (valid)
        {
            valid = !msg.IsAdMessage();
        }

        return valid;
    }

    /// <summary>
    /// 判断输入文本是否为合法名字（不包含任何换行符，广告），注意不判断是否重复
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static bool IsValidName(this string name)
    {
        bool valid = !name.IsNewLineMessage();

        if (valid)
        {
            valid = !name.IsAdMessage();
        }

        return valid;
    }

    /// <summary>
    /// 限制文本长度，最多100个字符
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string ClampLength(this string text, int maxLength = 100)
    {
        if (text.Length > maxLength)
        {
            return text.Substring(0, maxLength);
        }
        else
        {
            return text;
        }
    }

    /// <summary>
    /// 构建N维树
    /// </summary>
    private static void BuildWorldBlockTree(string[] blockWordList)
    {
        wordBlockTree = new Dictionary<char, NTree>();
        foreach (string word in blockWordList)
        {
            NTree tree;
            if (wordBlockTree.TryGetValue(word[0], out tree))
            {
                if (word.Length == 1)
                {
                    tree.isEnd = true;
                }
                for (int i = 1; i < word.Length; i++)
                {
                    var c = word[i];
                    if (tree.children.ContainsKey(c) && tree.children[c].index == i)
                    {
                        tree = tree.children[c];
                        if (i == word.Length - 1)
                        {
                            tree.isEnd = true;
                        }
                        continue;
                    }
                    NTree child = new NTree();
                    child.c = c;
                    child.index = i;
                    child.isEnd = i == word.Length - 1;
                    tree.children.Add(c, child);
                    tree = child;
                }
            }
            else
            {
                tree = new NTree();
                tree.c = word[0];
                tree.index = 0;
                tree.isEnd = word.Length == 1;
                wordBlockTree.Add(tree.c, tree);

                for (int i = 1; i < word.Length; i++)
                {
                    var c = word[i];
                    NTree child = new NTree();
                    child.c = c;
                    child.index = i;
                    child.isEnd = i == word.Length - 1;
                    tree.children.Add(c, child);
                    tree = child;
                }
            }
        }
    }

    /// <summary>
    /// 过滤敏感词，返回结果是个元组，表示：过滤后的文本，是否包含敏感词
    /// </summary>
    /// <param name="str"></param>
    /// <param name="cache"></param>
    /// <returns></returns>
    public static (string filterResult, bool hasBlockWords) FilterBlockWords(this string str, bool cache = true)
    {
        if (string.IsNullOrEmpty(str))
        {
            return (str, false);
        }

        if (TextFileterdCache.TryGetValue(str, out var cachedResult))
        {
            return cachedResult;
        }

        string originStr = str;
        bool hasBeenBlocked = false;
        for (int checkIndex = 0; checkIndex < str.Length;)
        {
            var c = str[checkIndex];
            int checkStartIndex = checkIndex;
            if (wordBlockTree != null && wordBlockTree.TryGetValue(c, out NTree tree))
            {
                int blockWordLength = 0;
                if (!tree.isEnd && checkIndex < str.Length - 1)
                {
                    checkIndex++;
                    blockWordLength++;
                    c = str[checkIndex];
                    while (tree.children.ContainsKey(c) && tree.children[c].index == blockWordLength)
                    {
                        tree = tree.children[c];
                        if (checkIndex == str.Length - 1 || tree.isEnd)
                        {
                            checkIndex++;
                            blockWordLength++;
                            break;
                        }

                        checkIndex++;
                        blockWordLength++;
                        c = str[checkIndex];
                    }
                }
                else
                {
                    checkIndex++;
                    blockWordLength++;
                }

                if (tree.isEnd)
                {
                    str = str.Remove(checkStartIndex, blockWordLength);
                    str = str.Insert(checkStartIndex, "***");

                    checkIndex += (3 - blockWordLength);
                    hasBeenBlocked = true;
                }
                else
                {
                    checkIndex = checkStartIndex + 1;
                }
            }
            else
            {
                checkIndex++;
            }
        }
        var result = (str, hasBeenBlocked);
        if (cache)
        {
            TextFileterdCache.Add(originStr, result);
        }
        return result;
    }

    /// <summary>
    /// 判断输入文本是否包含换行符
    /// </summary>
    /// <returns></returns>
    public static bool IsNewLineMessage(this string msg)
    {
        return newLineRegex.IsMatch(msg);
    }

    /// <summary>
    /// 判断要输入文本是否在半小时内重复，判断时，要去除非常用汉字以外的所有字符
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static bool IsRepeatMessage(this string msg)
    {
        //首先去除非常用汉字以外的所有字符
        string msgPure = removeNotChineseCharacterRegex.Replace(msg, string.Empty);
        if (chatLimitInfoDict != null && chatLimitInfoDict.ContainsKey(msgPure))
        {
            long timeStamp = chatLimitInfoDict[msgPure];
            if (Math.Abs(TimerSystem.TimestampMillisecond - timeStamp) < RepeatMessageTimeRange)
            {
                Debug.Log($"发送失败（重复）：{msg}", "聊天过滤");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断输入的文本是否包含广告信息（使用过滤规则进行匹配）
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static bool IsAdMessage(this string msg)
    {
        if (filterRule != null)
        {
            var pinyin = Pinyin4Net.GetPinyin(msg, PinyinFormat.WITHOUT_TONE);
            //检测单条规则
            foreach (var singleRule in filterRule.singleRule)
            {
                bool matchRule = false;
                if (singleRule.matchPinYin)
                {
                    matchRule = singleRule.Match(pinyin);
                }
                else
                {
                    matchRule = singleRule.Match(msg);
                }
                if (matchRule)
                {
                    Debug.Log("RYH", "聊天过滤", $"\"{msg}\", 发送失败：包含广告词：{singleRule.matchedWord}");
                    return true;
                }
            }

            //检测组合规则
            foreach (var singleCombineRule in filterRule.combineRule)
            {
                int matchCount = 0;
                string adWord = string.Empty;
                foreach (var singleRule in singleCombineRule.rules)
                {
                    bool matchRule = false;
                    if (singleRule.matchPinYin)
                    {
                        matchRule = singleRule.Match(pinyin);
                    }
                    else
                    {
                        matchRule = singleRule.Match(msg);
                    }
                    if (matchRule)
                    {
                        matchCount++;
                        adWord += singleRule.matchedWord + "，";
                    }
                }

                if (matchCount >= singleCombineRule.minMatchCount)
                {
                    Debug.Log("RYH", "聊天过滤", $"\"{msg}\", 发送失败：包含广告词：{adWord}");
                    return true;
                }
            }
        }

        return false;
    }
}
