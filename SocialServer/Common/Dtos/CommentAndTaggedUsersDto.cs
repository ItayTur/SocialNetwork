﻿using Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Dtos
{
    public class CommentAndTaggedUsersDto
    {
        public CommentModel Comment { get; set; }
        public IEnumerable<UserModel> TaggedUsers { get; set; }
        public UserModel Writer { get; set; }
    }
}
