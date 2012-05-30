﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Reflection
{
	public class MissingMethodException : MissingMemberException
	{
		public MissingMethodException(string message)
			: base(message)
		{
		}
	}
}
