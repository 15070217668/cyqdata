
using CYQ.Data.Extension;
using CYQ.Data.Table;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace CYQ.Data.SQL
{
    /// <summary>
    /// Sql ����ʽ���� (�������ֹ���)
    /// </summary>
    internal class SqlFormat
    {
        /// <summary>
        /// Sql�ؼ��ִ���
        /// </summary>
        public static string Keyword(string name, DalType dalType)
        {
            if (!string.IsNullOrEmpty(name))
            {
                name = name.Trim();
                if (name.IndexOfAny(new char[] { ' ', '[', ']', '`', '"', '(', ')' }) == -1)
                {
                    string pre = null;
                    int i = name.LastIndexOf('.');// ���ӿ��֧�֣�demo.dbo.users��
                    if (i > 0)
                    {
                        string[] items = name.Split('.');
                        pre = items[0];
                        name = items[items.Length - 1];
                    }
                    switch (dalType)
                    {
                        case DalType.Access:
                            return "[" + name + "]";
                        case DalType.MsSql:
                        case DalType.Sybase:
                            return (pre == null ? "" : pre + "..") + "[" + name + "]";
                        case DalType.MySql:
                            return (pre == null ? "" : pre + ".") + "`" + name + "`";
                        case DalType.SQLite:
                            return "\"" + name + "\"";
                        case DalType.Txt:
                        case DalType.Xml:
                            return NotKeyword(name);
                    }
                }
            }
            return name;
        }
        /// <summary>
        /// ȥ���ؼ��ַ���
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string NotKeyword(string name)
        {
            name = name.Trim();
            if (name.IndexOfAny(new char[] { '(', ')' }) == -1 && name.Split(' ').Length == 1)
            {
                //string pre = string.Empty;
                int i = name.LastIndexOf('.');// ���ӿ��֧�֣�demo.dbo.users��
                if (i > 0)
                {
                    // pre = name.Substring(0, i + 1);
                    name = name.Substring(i + 1);
                }
                name = name.Trim('[', ']', '`', '"');

            }
            return name;
        }
        /// <summary>
        /// Sql���ݿ���ݺ�Sqlע�봦��
        /// </summary>
        public static string Compatible(object where, DalType dalType, bool isFilterInjection)
        {
            string text = GetIFieldSql(where);
            if (isFilterInjection)
            {
                text = SqlInjection.Filter(text, dalType);
            }
            text = SqlCompatible.Format(text, dalType);

            return RemoveWhereOneEqualsOne(text);
        }

        /// <summary>
        /// �Ƴ�"where 1=1"
        /// </summary>
        internal static string RemoveWhereOneEqualsOne(string sql)
        {
            try
            {
                sql = sql.Trim();
                if (sql == "where 1=1")
                {
                    return string.Empty;
                }
                if (sql.EndsWith(" and 1=1"))
                {
                    return sql.Substring(0, sql.Length - 8);
                }
                int i = sql.IndexOf("where 1=1", StringComparison.OrdinalIgnoreCase);
                //do
                //{
                if (i > 0)
                {
                    if (i == sql.Length - 9)//��where 1=1 ������
                    {
                        sql = sql.Substring(0, sql.Length - 10);
                    }
                    else if (sql.Substring(i + 10, 8).ToLower() == "order by")
                    {
                        sql = sql.Remove(i, 10);//�����ж����
                    }
                    // i = sql.IndexOf("where 1=1", StringComparison.OrdinalIgnoreCase);
                }
                //}
                //while (i > 0);
            }
            catch
            {

            }

            return sql;
        }

        /// <summary>
        /// ��������1=2��SQL���
        /// </summary>
        /// <param name="tableName">����������ͼ���</param>
        /// <returns></returns>
        internal static string BuildSqlWithWhereOneEqualsTow(string tableName)
        {
            tableName = tableName.Trim();
            if (tableName[0] == '(' && tableName.IndexOf(')') > -1)
            {
                int end = tableName.LastIndexOf(')');
                string sql = tableName.Substring(1, end - 1);//.Replace("\r\n", "\n").Replace('\n', ' '); ����ע�͵Ļ��С�
                if (sql.IndexOf(" where ", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    return Regex.Replace(sql, " where ", " where 1=2 and ", RegexOptions.IgnoreCase);
                }
                return sql + " where 1=2";
            }
            return string.Format("select * from {0} where 1=2", tableName);
        }

        /// <summary>
        /// Mysql Bit ���Ͳ��������������� ���ֶ�='0' �����ԣ�
        /// </summary>
        /// <param name="where"></param>
        /// <param name="mdc"></param>
        /// <returns></returns>
        internal static string FormatMySqlBit(string where, MDataColumn mdc)
        {
            if (where.Contains("'0'"))
            {
                foreach (MCellStruct item in mdc)
                {
                    int groupID = DataType.GetGroup(item.SqlType);
                    if (groupID == 1 || groupID == 3)//��ͼģʽ��ȡ����bit��bigint,��������һ������
                    {
                        if (where.IndexOf(item.ColumnName, StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            string pattern = " " + item.ColumnName + @"\s*=\s*'0'";
                            where = Regex.Replace(where, pattern, " " + item.ColumnName + "=0", RegexOptions.IgnoreCase);
                        }
                    }
                }
            }
            return where;
        }


        internal static List<string> GetTableNamesFromSql(string sql)
        {
            List<string> nameList = new List<string>();

            //��ȡԭʼ����
            string[] items = sql.Split(' ');
            if (items.Length == 1) { return nameList; }//������
            if (items.Length > 3) // ���ǰ����ո��select * from xxx
            {
                bool isKeywork = false;
                foreach (string item in items)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        string lowerItem = item.ToLower();
                        switch (lowerItem)
                        {
                            case "from":
                            case "update":
                            case "into":
                            case "join":
                            case "table":
                                isKeywork = true;
                                break;
                            default:
                                if (isKeywork)
                                {
                                    if (item[0] == '(' || item.IndexOf('.') > -1) { isKeywork = false; }
                                    else
                                    {
                                        isKeywork = false;
                                        nameList.Add(NotKeyword(item));
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            return nameList;
        }

        #region IField����

        /// <summary>
        /// ��̬�Ķ�IField�ӿڴ���
        /// </summary>
        public static string GetIFieldSql(object whereObj)
        {
            if (whereObj is IField)
            {
                IField filed = whereObj as IField;
                string where = filed.Sql;
                filed.Sql = "";
                return where;
            }
            return Convert.ToString(whereObj);
        }
        #endregion
    }
}
