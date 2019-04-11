using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace XML修改器.utiliy
{
    public class XMLHelper
    {

        static public List<model.ModelBase> GetAllTopicType1(string XmlPath)
        {
            List<model.ModelBase> TopicList = new List<model.ModelBase>();

            //将XML文件加载进来
            XDocument document = XDocument.Load(XmlPath);
            //获取到XML的根元素进行操作
            XElement root = document.Root;
            //获取根元素下的所有子元素
            IEnumerable<XElement> enumerable = root.Elements();
            for (int i = 0; i < enumerable.ToArray().Length; i++)
            {
                var item = enumerable.ToArray()[i];
                if (item.Name == "Config")
                {
                    continue;
                }
                model.ModelBase model = new XML修改器.model.ModelBase();
                //当前节点属性
                model.Topic = item.Attributes().ToArray()[0].Value;
                model.IsMultiptle = item.Attributes().ToArray()[1].Value == "0" ? true : false;
                model.Answer = item.Attributes().ToArray()[2].Value;
                model.Score = item.Attributes().ToArray()[3].Value;
                model.HavePicture = item.Attributes().ToArray()[4].Value == "0" ? false : true;
                model.PicName = item.Attributes().ToArray()[5].Value;
                model.Audioname = item.Attributes().ToArray()[6].Value;
                //子节点
                //A
                model.OptionA = item.Elements().ToArray()[0].Attributes().ToArray()[0].Value.Replace("A、", "");
                model.IsTex = item.Elements().ToArray()[0].Attributes().ToArray()[2].Value == "0" ? true : false;
                //B
                model.OptionB = item.Elements().ToArray()[1].Attributes().ToArray()[0].Value.Replace("B、", "");
                model.IsTex = item.Elements().ToArray()[1].Attributes().ToArray()[2].Value == "0" ? false : true;
                //C
                if (item.Elements().ToArray().Length >= 3)
                {
                    model.OptionC = item.Elements().ToArray()[2].Attributes().ToArray()[0].Value.Replace("C、", "");
                    model.IsTex = item.Elements().ToArray()[2].Attributes().ToArray()[2].Value == "0" ? false : true;
                }
                else
                {
                    model.OptionC = "";
                }

                //D
                if (item.Elements().ToArray().Length >= 4)
                {
                    model.OptionD = item.Elements().ToArray()[3].Attributes().ToArray()[0].Value.Replace("D、", "");
                    model.IsTex = item.Elements().ToArray()[3].Attributes().ToArray()[2].Value == "0" ? true : false;
                }
                else
                {
                    model.OptionD = "";
                }
                TopicList.Add(model);
            }
            return TopicList;
        }
        static public List<model.ModelType2> GetAllTopicType2(string XmlPath)
        {
            List<model.ModelType2> TopicList = new List<model.ModelType2>();

            //将XML文件加载进来
            XDocument document = XDocument.Load(XmlPath);
            //获取到XML的根元素进行操作
            XElement root = document.Root;
            //获取根元素下的所有子元素
            IEnumerable<XElement> enumerable = root.Elements();
            for (int i = 0; i < enumerable.ToArray().Length; i++)
            {
                var item = enumerable.ToArray()[i];
                if (item.Name == "Config")
                {
                    continue;
                }
                model.ModelType2 model = new XML修改器.model.ModelType2();
                //当前节点属性
                model.Style = item.Attributes().ToArray()[0].Value;
                model.Audioname = item.Attributes().ToArray()[1].Value;
                model.Topic = item.Attributes().ToArray()[2].Value.Replace("、","");
                model.Answer = item.Attributes().ToArray()[3].Value;
                //A
                model.OptionA = item.Attributes().ToArray()[4].Value.Replace("A、", "");
                //B
                model.OptionB = item.Attributes().ToArray()[5].Value.Replace("B、", "");
                //C
                 model.OptionC =  item.Attributes().ToArray()[6].Value.Replace("C、", "");
                //D
                 model.OptionD = item.Attributes().ToArray()[7].Value.Replace("D、", "");
                TopicList.Add(model);
            }
            return TopicList;
        }
        static public void GenerateXML(string XmlPath,List<model.ModelBase>  lists,string count,string waittime)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<Root>");
            sb.AppendLine("            <Config count=\"" + count + "\" waittime=\"" + waittime + "\"></Config>");
            for (int i = 0; i < lists.Count; i++)
            {
                sb.AppendLine("            <Question title= \"" + lists[i].Topic + "\" isSingle=\"" + (lists[i].IsMultiptle == true ? "0" : "1") + "\" answer=\"" + lists[i].Answer + "\" score=\"" + lists[i].Score + "\" havePicture=\"" + (lists[i].HavePicture==true?"1":"0") + "\" picName=\"" + lists[i].PicName + "\" audioName=\"" + lists[i].Audioname + "\">");
                sb.AppendLine("                              <Option content= \"A、" + lists[i].OptionA + "\" Sign=\"A\"  isTex = \"0\"/>");
                sb.AppendLine("                              <Option content= \"B、" + lists[i].OptionB + "\" Sign=\"B\"  isTex = \"0\" />");
                if(!string.IsNullOrWhiteSpace(lists[i].OptionC))
                {
                    sb.AppendLine("                              <Option content= \"C、" + lists[i].OptionC + "\" Sign=\"C\"   isTex = \"0\" />");
                }
                if (!string.IsNullOrWhiteSpace(lists[i].OptionD))
                {
                    sb.AppendLine("                              <Option content= \"D、" + lists[i].OptionD + "\" Sign=\"D\"   isTex = \"0\" />");
                }
                sb.AppendLine("            </Question>");
            }
            sb.AppendLine("</Root>");
            FileHelper.Delete(XmlPath);
            FileHelper.SaveFile_Append(XmlPath, sb.ToString(), sb.Length);
        }

        static public void GenerateXMLType2(string XmlPath, List<model.ModelType2> lists)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<Root>");
            for (int i = 0; i < lists.Count; i++)
            {
                sb.AppendLine("            <Question Style = \"" + lists[i].Style
                    +"\" AudioName = \"" + lists[i].Audioname
                    + "\" Title=\"、" + lists[i].Topic
                    + "\" Answer=\"" + lists[i].Answer
                    + "\" OptionA=\"" + (lists[i].OptionA == "" ? "" : "A、" + lists[i].OptionA)
                    + "\" OptionB=\"" + (lists[i].OptionB == "" ? "" : "B、" + lists[i].OptionB)
                    + "\" OptionC=\"" + (lists[i].OptionC == "" ? "" : "C、" + lists[i].OptionC)
                    + "\" OptionD=\"" + (lists[i].OptionD == "" ? "" : "D、" + lists[i].OptionD)
                    +"\" />");
                sb.AppendLine("            </Question>");
            }
            sb.AppendLine("</Root>");
            FileHelper.Delete(XmlPath);
            FileHelper.SaveFile_Append(XmlPath, sb.ToString(), sb.Length);
        }
        public static object Deserialize(Type type, string xml)
        {
            using (StringReader sr = new StringReader(xml))
            {
                XmlSerializer xmldes = new XmlSerializer(type);
                return xmldes.Deserialize(sr);
            }
        } 
    }
}
