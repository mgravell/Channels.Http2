﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Channels.Samples.Models
{
    public class Pet
    {
        public int Id { get; set; }

        public int Age { get; set; }

        public Category Category { get; set; }

        public bool HasVaccinations { get; set; }

        public string Name { get; set; }

        public List<Image> Images { get; set; }

        public List<Tag> Tags { get; set; }

        public string Status { get; set; }
    }

    public class Image
    {
        public int Id { get; set; }

        public string Url { get; set; }
    }

    public class Tag
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
