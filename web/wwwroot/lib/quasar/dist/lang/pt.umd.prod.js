/*!
 * Quasar Framework v2.12.5
 * (c) 2015-present Razvan Stoenescu
 * Released under the MIT License.
 */
(function(e,a){"object"===typeof exports&&"undefined"!==typeof module?module.exports=a():"function"===typeof define&&define.amd?define(a):(e="undefined"!==typeof globalThis?globalThis:e||self,e.Quasar=e.Quasar||{},e.Quasar.lang=e.Quasar.lang||{},e.Quasar.lang.pt=a())})(this,function(){"use strict";var e={isoName:"pt",nativeName:"Português",label:{clear:"Limpar",ok:"OK",cancel:"Cancelar",close:"Fechar",set:"Marcar",select:"Escolher",reset:"Limpar",remove:"Remover",update:"Atualizar",create:"Criar",search:"Procurar",filter:"Filtrar",refresh:"Recarregar",expand:e=>e?`Expandir "${e}"`:"Expandir",collapse:e=>e?`Recolher "${e}"`:"Colapso"},date:{days:"Domingo_Segunda-feira_Terça-feira_Quarta-feira_Quinta-feira_Sexta-feira_Sábado".split("_"),daysShort:"Dom_Seg_Ter_Qua_Qui_Sex_Sáb".split("_"),months:"Janeiro_Fevereiro_Março_Abril_Maio_Junho_Julho_Agosto_Setembro_Outubro_Novembro_Dezembro".split("_"),monthsShort:"Jan_Fev_Mar_Abr_Mai_Jun_Jul_Ago_Set_Out_Nov_Dez".split("_"),firstDayOfWeek:1,format24h:!0,pluralDay:"dias"},table:{noData:"Sem dados disponíveis",noResults:"Não foi encontrado nenhum resultado",loading:"A carregar...",selectedRecords:e=>e>0?e+" linha"+(1===e?" selecionada":"s selecionadas")+".":"Nenhuma linha selecionada.",recordsPerPage:"Linhas por página:",allRows:"Todas",pagination:(e,a,o)=>e+"-"+a+" de "+o,columns:"Colunas"},editor:{url:"URL",bold:"Negrito",italic:"Itálico",strikethrough:"Rasurado",underline:"Sublinhado",unorderedList:"Lista não-ordenada",orderedList:"Lista ordenada",subscript:"Subscrito",superscript:"Sobrescrito",hyperlink:"Hyperlink",toggleFullscreen:"Maximizar",quote:"Citação",left:"Alinhado à esquerda",center:"Alinhado ao centro",right:"Alinhado à direita",justify:"Justificado",print:"Imprimir",outdent:"Diminuir indentação",indent:"Aumentar indentação",removeFormat:"Remover formatação",formatting:"Formatação",fontSize:"Tamanho do tipo de letra",align:"Alinhar",hr:"Inserir linha horizontal",undo:"Desfazer",redo:"Refazer",heading1:"Cabeçalho 1",heading2:"Cabeçalho 2",heading3:"Cabeçalho 3",heading4:"Cabeçalho 4",heading5:"Cabeçalho 5",heading6:"Cabeçalho 6",paragraph:"Parágrafo",code:"Código",size1:"Muito pequeno",size2:"Pequeno",size3:"Normal",size4:"Médio",size5:"Grande",size6:"Enorme",size7:"Máximo",defaultFont:"Tipo de letra padrão",viewSource:"Exibir fonte"},tree:{noNodes:"Sem nós disponíveis",noResults:"Nenhum resultado encontrado"}};return e});