﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using TestDriverCodePack;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

namespace TestDriver
{
    //-------------------------------------------------------------------------------------------------------------------------
    //
    // Prerequisites:
    //      PropertyHandler registered and set up on .txt extension
    //      Our bitness (32/64) matches bitness of FileMeta setup.  32-bit on 64-bit Windows should work.
    //
    //-------------------------------------------------------------------------------------------------------------------------

    // Write a single property using API COde Pack (which uses the Property Handler), and read it back directly using the Property Handler
    public class RoundTrip1 : Test
    {
        public override string Name { get { return "Write API Code Pack, read Property handler"; } }

        public override bool RunBody(State state)
        {
            RequirePropertyHandlerRegistered();
            RequireTxtProperties();

            const string cval = "acomment!!";
            string propertyName = "System.Comment";

            //Create a temp file to put metadata on
            string fileName = CreateFreshFile(1);

            // Use API Code Pack to set the value
            IShellProperty prop = ShellObject.FromParsingName(fileName).Properties.GetProperty(propertyName);
            (prop as ShellProperty<string>).Value = cval;
            string svalue = null;

            var handler = new CPropertyHandler();
            handler.Initialize(fileName, 0);

            PropVariant value = new PropVariant();
            handler.GetValue(new TestDriverCodePack.PropertyKey(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), 6), value);
            svalue = (string)value.Value;

            Marshal.ReleaseComObject(handler);  // preempt GC for CCW

            File.Delete(fileName);  // only works if all have let go of the file

            return svalue == cval;
        }
    }

    // Write a single property directly using the Property Handler, and read it back using API COde Pack (which uses the Property Handler)
    public class RoundTrip2 : Test
    {
        public override string Name { get { return "Write Property handler, read API Code Pack"; } }

        public override bool RunBody(State state)
        {
            RequirePropertyHandlerRegistered();
            RequireTxtProperties();

            const string cval = "bcomment??";
            string propertyName = "System.Comment";

            //Create a temp file to put metadata on
            string fileName = CreateFreshFile(1);

            var handler = new CPropertyHandler();
            handler.Initialize(fileName, 0);

            PropVariant value = new PropVariant(cval);
            handler.SetValue(new TestDriverCodePack.PropertyKey(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), 6), value);
            handler.Commit();

            Marshal.ReleaseComObject(handler);  // preempt GC for CCW

            // Use API Code Pack to read the value
            IShellProperty prop = ShellObject.FromParsingName(fileName).Properties.GetProperty(propertyName);
            object oval = prop.ValueAsObject;

            File.Delete(fileName);  // only works if all have let go of the file

            return (string)oval == cval;
        }
    }

    // Write a single property using DSOFile (which does not use the Property Handler), and read it back directly using the Property Handler
    // This only does anything in 32-bit, since DSOFile only ships as 32-bit
    public class RoundTrip3 : Test
    {
        public override string Name { get { return "Write DSOFile, read Property handler"; } }

        public override bool RunBody(State state)
        {
            RequirePropertyHandlerRegistered();
            RequireTxtProperties();

#if x86

            const string cval = "ccomment***";

            //Create a temp file to put metadata on
            string fileName = CreateFreshFile(1);

            // Use DSOFile to set the value
            var dso = new DSOFile.OleDocumentProperties();
            dso.Open(fileName);
            var sum = dso.SummaryProperties;
            sum.Comments = cval;
            dso.Save();
            dso.Close();
            Marshal.ReleaseComObject(dso);  // preempt GC for CCW

            string svalue = null;

            var handler = new CPropertyHandler();
            handler.Initialize(fileName, 0);

            PropVariant value = new PropVariant();
            handler.GetValue(new TestDriverCodePack.PropertyKey(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), 6), value);
            svalue = (string)value.Value;

            Marshal.ReleaseComObject(handler);  // preempt GC for CCW

            File.Delete(fileName);  // only works if all have let go of the file

            return svalue == cval;
#else
            state.RecordEntry("Test skipped because DSOFile is 32-bit only");
            return true;
#endif
        }
    }
}