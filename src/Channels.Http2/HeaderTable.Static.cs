using System.Collections.Generic;
using System.IO;

namespace Channels.Http2
{
    partial struct HeaderTable
    {
        static readonly string[] _staticHeaderNames = SplitHeaders(
@":authority                 
:method                    
:method                    
:path                      
:path                      
:scheme                    
:scheme                    
:status                    
:status                    
:status                    
:status                    
:status                    
:status                    
:status                    
accept-charset             
accept-encoding            
accept-language            
accept-ranges              
accept                     
access-control-allow-origin
age                        
allow                      
authorization              
cache-control              
content-disposition        
content-encoding           
content-language           
content-length             
content-location           
content-range              
content-type               
cookie                     
date                       
etag                       
expect                     
expires                    
from                       
host                       
if-match                   
if-modified-since          
if-none-match              
if-range                   
if-unmodified-since        
last-modified              
link                       
location                   
max-forwards               
proxy-authenticate         
proxy-authorization        
range                      
referer                    
refresh                    
retry-after                
server                     
set-cookie                 
strict-transport-security  
transfer-encoding          
user-agent                 
vary                       
via                        
www-authenticate           
"),
            _staticHeaderValues = SplitHeaders(
@"
GET          
POST         
/            
/index.html  
http         
https        
200          
204          
206          
304          
400          
404          
500          
             
gzip, deflate");

        static readonly uint _staticTableLength = (uint)_staticHeaderNames.Length;

        private static string[] SplitHeaders(string lines)
        {
            List<string> headers = new List<string>();
            using (var reader = new StringReader(lines))
            {
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    headers.Add(line.Trim());
                }
            }
            return headers.ToArray();
        }

        
        private Header GetStaticHeader(uint index)
        {
            return new Header(
                name: _staticHeaderNames[index],
                value: (index < _staticHeaderValues.Length ? _staticHeaderValues[index] : "")
            );
        }

    }
}
