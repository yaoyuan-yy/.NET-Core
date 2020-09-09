using System;
using System.Collections.Generic;
using System.Text;

namespace Model
{
    public class ReturnModel<T>
    {
        /// <summary>
        /// 是否操作成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 返回信息
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 返回数据集合
        /// </summary>
        public List<T> Data { get; set; }
    }
}
