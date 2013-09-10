Nessus Conversion Tool
===============

A tool for taking Nessus v2 output and converting it into something else.  Either through a GUI or command line.

Setup program available here: https://s3.amazonaws.com/nessus-conversion/NessusConversionSetup.msi

Exclusions File
===============

An exclusions file looks like this:

<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<exclusions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
	<exclusion pluginid="xxxxx" ipaddress="xxx.xxx.xxx.xxx" port="xxxxx" disdate="xx/xx/xxxx" expdate="zz/zz/zzzz"></exclusion>
	<exclusion pluginid="yyyyy" ipaddress="yyy.yyy.yyy.yyy" port="yyyyy" disdate="yy/yy/yyyy" expdate="ww/ww/wwww"></exclusion>
</exclusions>

Hopefully, the fields are self explanatory.  At this time, there is no way to put this data in via the program.  
In the future, I may make a form to do so, if there is enough interest.

I usually import something similar the above example into Excel, add as many rows as I like, and then export it back out.
The format does not allow for comments or CDATA.  Also, don't use leading zeros for the plugins or ports attributes. 

Anything put between the <exclusion> and the </exclusion> is text that will be added to a column on the exclusions 
worksheet.  But I may consider a means to attach screenshots or other evidence.

The attributes disdate and expdate are Discovery Date and Expiration Date.  Also, self explanatory I hope.  Although, origination date would have been more appropriate than discovery date.
