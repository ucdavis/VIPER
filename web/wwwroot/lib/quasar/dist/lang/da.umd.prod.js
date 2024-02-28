/*!
 * Quasar Framework v2.14.1
 * (c) 2015-present Razvan Stoenescu
 * Released under the MIT License.
 */
(function(e,r){"object"===typeof exports&&"undefined"!==typeof module?module.exports=r():"function"===typeof define&&define.amd?define(r):(e="undefined"!==typeof globalThis?globalThis:e||self,e.Quasar=e.Quasar||{},e.Quasar.lang=e.Quasar.lang||{},e.Quasar.lang.da=r())})(this,function(){"use strict";var e={isoName:"da",nativeName:"Dansk",label:{clear:"Ryd",ok:"OK",cancel:"Annuller",close:"Luk",set:"Sæt",select:"Vælg",reset:"Nulstil",remove:"Fjern",update:"Opdater",create:"Opret",search:"Søg",filter:"Filtrer",refresh:"Opdater",expand:e=>e?`Udvid "${e}"`:"Udvide",collapse:e=>e?`Skjul "${e}"`:"Bryder sammen"},date:{days:"Søndag_Mandag_Tirsdag_Onsdag_Torsdag_Fredag_Lørdag".split("_"),daysShort:"Søn_Man_Tirs_Ons_Tors_Fre_Lør".split("_"),months:"Januar_Februar_Marts_April_Maj_Juni_Juli_August_September_Oktober_November_December".split("_"),monthsShort:"Jan_Feb_Mar_Apr_Maj_Jun_Jul_Aug_Sep_Okt_Nov_Dec".split("_"),firstDayOfWeek:1,format24h:!0,pluralDay:"dage"},table:{noData:"Ingen data tilgængelig",noResults:"Ingen matchende resultater fundet",loading:"Indlæser...",selectedRecords:e=>1===e?"1 række valgt.":(0===e?"Ingen":e)+" rækker valgt.",recordsPerPage:"Rækker per side:",allRows:"Alle",pagination:(e,r,t)=>e+"-"+r+" af "+t,columns:"Kolonner"},editor:{url:"URL",bold:"Fed",italic:"Kursiv",strikethrough:"Gennemstreget",underline:"Understreget",unorderedList:"Punktliste",orderedList:"Numreret liste",subscript:"Sænket",superscript:"Hævet",hyperlink:"Hyperlink",toggleFullscreen:"Skift fuldskærm",quote:"Citat",left:"Venstrejustering",center:"Centreret",right:"Højrejustering",justify:"Lige margener",print:"Udskriv",outdent:"Formindsk indrykning",indent:"Forøg indrykning",removeFormat:"Fjern formattering",formatting:"Formattering",fontSize:"Skriftstørrelse",align:"Justering",hr:"Indsæt vandret streg",undo:"Fortryd",redo:"Gendan",heading1:"Overskrift 1",heading2:"Overskrift 2",heading3:"Overskrift 3",heading4:"Overskrift 4",heading5:"Overskrift 5",heading6:"Overskrift 6",paragraph:"Afsnit",code:"Kode",size1:"Meget lille",size2:"Lille",size3:"Normal",size4:"Mellemstor",size5:"Stor",size6:"Meget stor",size7:"Størst",defaultFont:"Standard skrifttype",viewSource:"Se kilde"},tree:{noNodes:"Ingen noder tilgængelige",noResults:"Ingen matchende noder fundet"}};return e});