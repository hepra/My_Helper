using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using WageCalculater.DAL;

namespace WageCalculater
{
    public static  class ExcleHelper
       {

           /// <summary>
           /// 
           /// </summary>
           /// <param name="dt">需要导出到excel的数据表dt</param>
           /// <param name="saveFileName">存储文件名</param>
           /// <returns></returns>
           public static void DataTableToExcel(System.Data.DataTable dt, string saveFileName)
           {
               if (dt == null) return;
               //-***************获取excel对象***************
               //  string saveFileName = "";
               TimeSpan dateBegin = new TimeSpan(DateTime.Now.Ticks);
               bool fileSaved = false;
               SaveFileDialog saveDialog = new SaveFileDialog();
               saveDialog.DefaultExt = "xls";
               saveDialog.Filter = "Excel文件|*.xls";
               saveDialog.FileName = "工资单";
               saveDialog.ShowDialog();
               saveFileName = saveDialog.FileName;
               if (saveFileName.IndexOf(":") < 0) return; //被点了取消
               Microsoft.Office.Interop.Excel.Application xlApp = new Microsoft.Office.Interop.Excel.Application();
               if (xlApp == null)
               {
                   MessageBox.Show("无法启动Excel，可能您未安装Excel");
                   return;
               }
               Microsoft.Office.Interop.Excel.Workbook workbook = xlApp.Workbooks.Add(true);
               Microsoft.Office.Interop.Excel.Worksheet worksheet = (Microsoft.Office.Interop.Excel.Worksheet)workbook.Worksheets[1];
               Microsoft.Office.Interop.Excel.Range range;

               // 列索引，行索引，总列数，总行数                   
               int colIndex = 0;
               int RowIndex = 0;
               int colCount = dt.Columns.Count;
               int RowCount = dt.Rows.Count;


               // *****************获取数据*********************

               // 创建缓存数据
               object[,] objData = new object[RowCount + 1, colCount];
               // 获取列标题
               for (int i = 0; i < dt.Columns.Count; i++)
               {
                   switch (dt.Columns[i].Caption)
                   {
                       case "ID": objData[RowIndex, i] = "ID"; break;
                       case "IDNum": objData[RowIndex, i] = "身份证号码"; break;
                       case "StaffName": objData[RowIndex, i] = "姓名"; break;
                       case "YearMonth": objData[RowIndex, i] = "年月"; break;
                       case "BaseWage": objData[RowIndex, i] = "基本工资"; break;
                       case "CountOfTypeQP": objData[RowIndex, i] = "清晰排板月汇总"; break;
                       case "CountOfTypeQC": objData[RowIndex, i] = "清晰拆板月汇总"; break;
                       case "CountOfTypeJP": objData[RowIndex, i] = "镓板排板月汇总"; break;
                       case "CountOfTypeJC": objData[RowIndex, i] = "镓板拆板月汇总"; break;
                       case "DockWage": objData[RowIndex, i] = "不良品扣除金额总和"; break;
                       case "FineOfCustomerComplaints": objData[RowIndex, i] = "客户投诉罚金总和"; break;
                       case "OverTime": objData[RowIndex, i] = "加班时间汇总"; break;
                       case "SocialSecurity": objData[RowIndex, i] = "社保扣除"; break;
                       case "WaterElectricity": objData[RowIndex, i] = "水电费扣除"; break;
                       case "WagePay": objData[RowIndex, i] = "总计"; break;
                       case "OtherNote": objData[RowIndex, i] = "备注"; break;
                   }
               }

               // 获取具体数据
               for (RowIndex = 0; RowIndex < RowCount; RowIndex++)
               {
                   for (colIndex = 0; colIndex < colCount; colIndex++)
                   {
                       if (dt.Columns[colIndex].Caption =="IDNum")
                       {
                           objData[RowIndex + 1, colIndex] = dt.Rows[RowIndex][colIndex].ToString();
                           continue;
                       }
                       objData[RowIndex + 1, colIndex] = dt.Rows[RowIndex][colIndex];
                   }

               }


               //*******************设置输出格式******************************

               //设置顶部説明   合并的单元格
               range = worksheet.Range[xlApp.Cells[1, 1], xlApp.Cells[1, colCount]];
               range.MergeCells = true;
               range.RowHeight = 38;
               range.Font.Bold = true;
               range.Font.Size = 14;
               range.Font.ColorIndex = 10;//字体颜色
               xlApp.ActiveCell.FormulaR1C1 = "员工工资汇总";

               //特殊数字格式
             //  range = worksheet.Range[xlApp.Cells[2, colCount], xlApp.Cells[RowCount, colCount]];


               xlApp.Cells.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
               range = worksheet.Range[xlApp.Cells[2, 1], xlApp.Cells[2, colCount]];

               range.Font.Bold = true;
               range.RowHeight = 20;


               //********************* 写入Excel*******************

               range = worksheet.Range[xlApp.Cells[2, 1], xlApp.Cells[RowCount + 2, colCount]];
               range.Value2 = objData;

               //***************************保存**********************

               if (saveFileName != "")
               {
                   try
                   {
                       workbook.Saved = true;
                       workbook.SaveCopyAs(saveFileName);
                       fileSaved = true;
                   }
                   catch (Exception ex)
                   {
                       fileSaved = false;
                       MessageBox.Show("导出文件时出错,文件可能正被打开！\n" + ex.Message);
                   }
               }
               else
               {
                   fileSaved = false;
               }
               xlApp.Quit();
               GC.Collect();//强行销毁   

               TimeSpan dateEnd = new TimeSpan(DateTime.Now.Ticks);
               TimeSpan tspan = dateBegin.Subtract(dateEnd).Duration();
               MessageBox.Show("导出成功，用时" + tspan.ToString() + "秒");
               if (fileSaved && System.IO.File.Exists(saveFileName))
                   System.Diagnostics.Process.Start(saveFileName); //保存成功后打开此文件
           }


           /// <summary>    

           /// 转化一个DataTable    

           /// </summary>    

           /// <typeparam name="T"></typeparam>    
           /// <param name="list"></param>    
           /// <returns></returns>    
           public static DataTable ToDataTable<T>(this IEnumerable<T> list)
           {

               //创建属性的集合    
               List<PropertyInfo> pList = new List<PropertyInfo>();
               //获得反射的入口    

               Type type = typeof(T);
               DataTable dt = new DataTable();
               //把所有的public属性加入到集合 并添加DataTable的列    
               Array.ForEach<PropertyInfo>(type.GetProperties(), p => { pList.Add(p); dt.Columns.Add(p.Name, p.PropertyType); });
               foreach (var item in list)
               {
                   //创建一个DataRow实例    
                   DataRow row = dt.NewRow();
                   //给row 赋值    
                   pList.ForEach(p => row[p.Name] = p.GetValue(item, null));
                   //加入到DataTable    
                   dt.Rows.Add(row);
               }
               return dt;
           }


           /// <summary>    
           /// DataTable 转换为List 集合    
           /// </summary>    
           /// <typeparam name="TResult">类型</typeparam>    
           /// <param name="dt">DataTable</param>    
           /// <returns></returns>    
           public static List<T> ToList<T>(this DataTable dt) where T : class, new()
           {
               //创建一个属性的列表    
               List<PropertyInfo> prlist = new List<PropertyInfo>();
               //获取TResult的类型实例  反射的入口    

               Type t = typeof(T);

               //获得TResult 的所有的Public 属性 并找出TResult属性和DataTable的列名称相同的属性(PropertyInfo) 并加入到属性列表     
               Array.ForEach<PropertyInfo>(t.GetProperties(), p => { if (dt.Columns.IndexOf(p.Name) != -1) prlist.Add(p); });

               //创建返回的集合    

               List<T> oblist = new List<T>();

               foreach (DataRow row in dt.Rows)
               {
                   //创建TResult的实例    
                   T ob = new T();
                   //找到对应的数据  并赋值    
                   prlist.ForEach(p => { if (row[p.Name] != DBNull.Value) p.SetValue(ob, row[p.Name], null); });
                   //放入到返回的集合中.    
                   oblist.Add(ob);
               }
               return oblist;
           }  
       }

}
