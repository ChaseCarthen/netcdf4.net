/*
 * Author Luke Campbell <LCampbelL@ASAscience.com>
 * netcdf4.test
 */
using System;
using System.Collections.Generic;
using netcdf4;

namespace netcdf4.test
{
    class RunTests
    {
        public static void Main (string[] args)
        {
            List<UnitTest> tests = new List<UnitTest> { 
                new TestNcFile(),
                new TestNcGroup(),
                new TestNcAtt(),
                new TestNcType(),
                new TestNcDim(),
                new TestNcVar()
            };
            
            foreach(UnitTest test in tests) {
                test.Run();
            }
        }
    }
}
