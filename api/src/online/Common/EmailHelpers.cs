namespace Online.Common;

public static class EmailHelpers
{
    private const string EmailTemplate = """
    <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
    <html lang="en" dir="ltr" xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:v="urn:schemas-microsoft-com:vml">

    <head>
      <meta http-equiv="Content-Type" content="text/html; charset=UTF-8">
      <meta name="viewport" content="width=device-width, initial-scale=1, user-scalable=yes">
      <meta name="x-apple-disable-message-reformatting">
      <meta name="format-detection" content="telephone=no, date=no, address=no, email=no, url=no">
      <meta name="viewport" content="width=device-width, initial-scale=1, user-scalable=yes">
      <meta name="x-apple-disable-message-reformatting">
      <meta name="format-detection" content="telephone=no, date=no, address=no, email=no, url=no"><!--[if mso]><xml><o:OfficeDocumentSettings><o:AllowPNG /><o:PixelsPerInch>96</o:PixelsPerInch></o:OfficeDocumentSettings></xml><![endif]/-->
    </head>

    <body style="background-color:#f6f9fc;font-family:-apple-system,BlinkMacSystemFont,&quot;Segoe UI&quot;,Roboto,&quot;Helvetica Neue&quot;,Ubuntu,sans-serif">
      <div data-skip="true" style="display:none;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden">Here is a preview<div> ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿ ‌​‍‎‏﻿</div>
      </div>
      <div style="table-layout:fixed;width:100%">
        <div style="margin:0 auto;max-width:600px"><span><!--[if mso]><table align="center" width="600" style="border-spacing: 0; width:600px;" role="presentation"><tr><td><![endif]--></span>
          <table align="center" width="100%" role="presentation" cellspacing="0" cellpadding="0" border="0" style="max-width:600px;width:580px;margin:30px auto;background-color:#ffffff">
            <tbody>
              <tr style="width:100%">
                <td align="center">
                  <table align="center" width="100%" style="display:flex;justify-content:center;aling-items:center;padding:30px" border="0" cellpadding="0" cellspacing="0" role="presentation">
                    <tbody>
                      <tr>
                        <td><img alt="Company Name" src="https://pos.aviatesoft.com/logo.svg" width="200" height="50" style="border:0;display:block;outline:none;text-decoration:none;max-width:100%"></td>
                      </tr>
                    </tbody>
                  </table>
                  <table align="center" width="100%" style="width:100%;display:flex" border="0" cellpadding="0" cellspacing="0" role="presentation">
                    <tbody>
                      <tr>
                        <td>
                          <table align="center" width="100%" role="presentation" cellspacing="0" cellpadding="0" border="0">
                            <tbody style="width:100%">
                              <tr style="width:100%">
                                <td style="border-bottom:1px solid rgb(238,238,238);width:249px"></td>
                                <td style="border-bottom:1px solid #2196F3;width:102px"></td>
                                <td style="border-bottom:1px solid rgb(238,238,238);width:249px"></td>
                              </tr>
                            </tbody>
                          </table>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                  <table align="center" width="100%" style="padding:5px 30px 10px 30px" border="0" cellpadding="0" cellspacing="0" role="presentation">
                    <tbody>
                      <tr>
                        <td>
                            {{body}}
                        </td>
                      </tr>
                    </tbody>
                  </table>
                  <table align="center" width="100%" style="width:100%;display:flex;margin-bottom:20px" border="0" cellpadding="0" cellspacing="0" role="presentation">
                    <tbody>
                      <tr>
                        <td>
                          <table align="center" width="100%" role="presentation" cellspacing="0" cellpadding="0" border="0">
                            <tbody style="width:100%">
                              <tr style="width:100%">
                                <td style="border-bottom:1px solid rgb(238,238,238);width:249px"></td>
                                <td style="border-bottom:1px solid #2196F3;width:102px"></td>
                                <td style="border-bottom:1px solid rgb(238,238,238);width:249px"></td>
                              </tr>
                            </tbody>
                          </table>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                  <table align="center" width="100%" style="width:580px;margin:0 auto" border="0" cellpadding="0" cellspacing="0" role="presentation">
                    <tbody>
                      <tr>
                        <td>
                          <p style="font-size:14px;line-height:24px;margin:16px 0;text-align:center;color:#706a7b">© {{year}} AviateSoft, All Rights Reserved</p>
                          <table align="center" width="100%" role="presentation" cellspacing="0" cellpadding="0" border="0">
                            <tbody style="width:100%">
                              <tr style="width:100%"></tr>
                            </tbody>
                          </table>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </td>
              </tr>
            </tbody>
          </table><span><!--[if mso]></td></tr></table><![endif]--></span>
        </div>
      </div>
    </body>
    </html>
    """;

    public static string EmailBody(string body)
    {
        return EmailTemplate.Replace("{{body}}", body).Replace("{{year}}", DateTime.UtcNow.Year.ToString());
    }
}
