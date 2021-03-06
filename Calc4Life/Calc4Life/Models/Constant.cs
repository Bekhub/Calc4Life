﻿using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace Calc4Life.Models
{
    [Table("Constants")]
    public class Constant
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public string Note { get; set; }
        public bool IsFavorite { get; set; }
    }
}
