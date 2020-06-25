({
   Id : "Sheepy.HalfPriceBionics",
   Name : "Half Price Bionics",
   Duration: "temp",
   Lang : "-",
   Author : "Sheepy",
   Version : "0.0.2020.0625",
   Description : "Half the price of all bionic augs in Blood and Titanium, which also half their repair cost.\n\nRequires Scripting Library (C#).",
   Url : "https://github.com/Sheep-y/PhoenixPt-Mods/",
   Requires: "Zy.cSharp",

   Actions : [{
      "Action" : "Default",
      "Phase" : "GeoscapeMod",
   },{
      "Eval" : 'void HalfPrice ( string guid ) {
         var costs = GetDef<ItemDef>( guid ).ManufacturePrice;
         foreach ( var e in costs.ToArray() ) { var cost = e; cost.Value = Mathf.RoundToInt( cost.Value / 2 ); costs.Set( cost ); }
      }',
   },{
      "Eval" : 'HalfPrice( "NJ_Exo_BIO_Helmet_BodyPartDef" )', // Disruptor Head
   },{
      "Eval" : 'HalfPrice( "NJ_Exo_BIO_Torso_BodyPartDef" )', // Neural Torso
   },{
      "Eval" : 'HalfPrice( "NJ_Exo_BIO_Legs_ItemDef" )', // Propeller Legs
   },{
      "Eval" : 'HalfPrice( "NJ_Jugg_BIO_Helmet_BodyPartDef" )', // Clarity Head
   },{
      "Eval" : 'HalfPrice( "NJ_Jugg_BIO_Torso_BodyPartDef" )', // Juggernaut Torso
   },{
      "Eval" : 'HalfPrice( "NJ_Jugg_BIO_Legs_ItemDef" )', // Armadillo Legs
   },{
      "Eval" : 'HalfPrice( "SY_Shinobi_BIO_Helmet_BodyPartDef" )', // Echo Head
   },{
      "Eval" : 'HalfPrice( "SY_Shinobi_BIO_Torso_BodyPartDef" )', // Vengeance Torso
   },{
      "Eval" : 'HalfPrice( "SY_Shinobi_BIO_Legs_ItemDef" )', // Mirage Legs
   }],
})