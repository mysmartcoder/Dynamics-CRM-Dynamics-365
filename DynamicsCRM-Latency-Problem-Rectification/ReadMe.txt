CaseStudy : Latency issue rectification in Dynamics CRM

Customer Problem : 
Client using dynamics CRM since many years. Entire business of client’s rely on Dynamics CRM. Many records in Lead,Contact,Opportunity entity. Customer had Latency issue as below.

=== Latency Test Info === 
Number of times run: 20
Run 1 time: 1594 ms
Run 2 time: 3294 ms
Run 3 time: 710 ms
Run 4 time: 3203 ms

Average latency: 3142 ms
Client Time: Thu, 28 May 2015 10:19:24 UTC

=== Bandwidth Test Info === 
Run 1
  Time: 6437 ms
  Blob Size: 15180 bytes
  Speed: 2 KB/sec
Run 2
  Time: 3194 ms
  Blob Size: 15180 bytes
  Speed: 4 KB/sec



Our Solution :

1. Run the following tool on both CRM Servers, being logged in as a user that has both CRM and domain administrator roles: http://support.microsoft.com/sdp/0B240C1D333431393931363330313D.
This [^] tool collects information regarding to your CRM Server deployment and configuration, such as installed files, registry keys, etc.
2. Run the following network diagnostic with a problematic user first when connecting to Production 1 and then when connecting to Production 2. http://<ORGANIZATION_URL>/tools/diagnostics/diag.aspx [^] 

3.Check following performance prerequisite.
RAM configuration at both the CRM server
Any extraneous processes and applications running on the same computer where the CRM1 installed
Review IIS Logging at Prod1 server.

4.Install "Microsoft Dynamics CRM 2013 Best Practices Analyzer" on server to perform below functions

	4.1.Gathers information about the CRM 2013 server roles that are installed on that server.
	4.2.Determines if the configurations are set according to the recommended best practices.
	4.3.Reports on all configurations, indicating settings that differ from recommendations.
	4.4.Indicates potential problems in the CRM 2013 features installed.
	4.5.Recommends solutions to potential problems.
	

