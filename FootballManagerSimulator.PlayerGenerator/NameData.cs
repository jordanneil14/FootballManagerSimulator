namespace FootballManagerSimulator.PlayerGenerator;

internal static class NameData
{
    internal static readonly Dictionary<string, (string[] FirstNames, string[] LastNames)> NationalityNames = new()
    {
        ["England"] = (
            ["James", "Jack", "Harry", "Oliver", "George", "Charlie", "Thomas", "William", "Daniel", "Alfie",
             "Ben", "Sam", "Ryan", "Luke", "Joe", "Callum", "Connor", "Aaron", "Lewis", "Nathan",
             "Josh", "Ethan", "Owen", "Reece", "Kyle", "Jake", "Liam", "Adam", "Marcus", "Declan",
             "Mason", "Harvey", "Archie", "Freddie", "Louie", "Tyler", "Brandon", "Jordan", "Jayden", "Riley"],
            ["Smith", "Jones", "Taylor", "Brown", "Wilson", "Williams", "Johnson", "Davies", "Robinson", "Wright",
             "Thompson", "Evans", "Walker", "White", "Roberts", "Green", "Hall", "Wood", "Jackson", "Clarke",
             "Harris", "Turner", "Martin", "Cooper", "Hill", "Ward", "Hughes", "Moore", "King", "Harrison",
             "Palmer", "Rice", "Saka", "Bellingham", "Foden", "Grealish", "Shaw", "Stones", "Maguire", "Pope"]),

        ["Spain"] = (
            ["Carlos", "Diego", "Pablo", "Sergio", "Alvaro", "Marcos", "Alejandro", "Iker", "Pedro", "Raul",
             "Fernando", "Andres", "David", "Jordi", "Gerard", "Dani", "Koke", "Rodri", "Gavi", "Pedri",
             "Marc", "Hugo", "Adrian", "Nacho", "Saul", "Mikel", "Aitor", "Unai", "Ander", "Jon"],
            ["Garcia", "Rodriguez", "Martinez", "Lopez", "Gonzalez", "Hernandez", "Perez", "Sanchez", "Romero", "Torres",
             "Ramos", "Ruiz", "Diaz", "Moreno", "Munoz", "Alvarez", "Jimenez", "Gutierrez", "Fernandez", "Castro",
             "Navarro", "Iglesias", "Ortiz", "Molina", "Serrano", "Delgado", "Reyes", "Vega", "Leon", "Prieto"]),

        ["France"] = (
            ["Antoine", "Kylian", "Hugo", "Lucas", "Paul", "Raphael", "Adrien", "Olivier", "Theo", "Aurelien",
             "Moussa", "Ousmane", "Kingsley", "N'Golo", "Benjamin", "Jules", "Dayot", "Ibrahima", "Eduardo", "Matteo",
             "Clement", "Rayan", "Enzo", "Youssef", "Maxime", "Florian", "Alexandre", "Pierre", "Nicolas", "Julien"],
            ["Dupont", "Martin", "Bernard", "Dubois", "Thomas", "Robert", "Richard", "Petit", "Durand", "Leroy",
             "Moreau", "Simon", "Laurent", "Lefebvre", "Michel", "Garcia", "David", "Bertrand", "Roux", "Vincent",
             "Fournier", "Morel", "Girard", "Andre", "Mercier", "Blanc", "Guerin", "Boyer", "Garnier", "Chevalier"]),

        ["Germany"] = (
            ["Thomas", "Manuel", "Leon", "Kai", "Timo", "Joshua", "Ilkay", "Leroy", "Niklas", "Antonio",
             "Marco", "Florian", "Julian", "Jamal", "Serge", "Jonas", "Lukas", "David", "Robin", "Maximilian",
             "Matthias", "Stefan", "Andreas", "Christoph", "Sebastian", "Philipp", "Bastian", "Tobias", "Jan", "Dennis"],
            ["Muller", "Schmidt", "Schneider", "Fischer", "Weber", "Meyer", "Wagner", "Becker", "Schulz", "Hoffmann",
             "Koch", "Richter", "Wolf", "Schroeder", "Neumann", "Schwarz", "Zimmermann", "Braun", "Kruger", "Hartmann",
             "Lange", "Werner", "Krause", "Lehmann", "Kohler", "Hermann", "Konig", "Huber", "Kaiser", "Fuchs"]),

        ["Italy"] = (
            ["Marco", "Gianluigi", "Giorgio", "Andrea", "Lorenzo", "Federico", "Nicolo", "Alessandro", "Matteo", "Gianluca",
             "Ciro", "Domenico", "Leonardo", "Giacomo", "Sandro", "Roberto", "Davide", "Simone", "Filippo", "Luca",
             "Stefano", "Paolo", "Antonio", "Giuseppe", "Francesco", "Massimo", "Fabio", "Giovanni", "Daniele", "Emanuele"],
            ["Rossi", "Russo", "Ferrari", "Esposito", "Bianchi", "Romano", "Colombo", "Ricci", "Marino", "Greco",
             "Bruno", "Gallo", "Conti", "Costa", "Giordano", "Mancini", "Rizzo", "Lombardi", "Moretti", "Barbieri",
             "Fontana", "Santoro", "Mariani", "Rinaldi", "Caruso", "Ferrara", "Gatti", "Pellegrini", "Palumbo", "Sanna"]),

        ["Brazil"] = (
            ["Neymar", "Gabriel", "Lucas", "Thiago", "Felipe", "Raphael", "Vinicius", "Rodrygo", "Casemiro", "Marquinhos",
             "Fabinho", "Roberto", "Richarlison", "Bruno", "Rafael", "Danilo", "Ederson", "Alisson", "Matheus", "Pedro",
             "Guilherme", "Leonardo", "Andre", "Diego", "Gustavo", "Eduardo", "Henrique", "Carlos", "Fernando", "Joao"],
            ["Silva", "Santos", "Oliveira", "Souza", "Lima", "Pereira", "Costa", "Ferreira", "Rodrigues", "Almeida",
             "Nascimento", "Araujo", "Melo", "Barbosa", "Ribeiro", "Martins", "Carvalho", "Rocha", "Gomes", "Mendes",
             "Nunes", "Moreira", "Vieira", "Cardoso", "Correia", "Teixeira", "Lopes", "Campos", "Monteiro", "Ramos"]),

        ["Argentina"] = (
            ["Lionel", "Angel", "Paulo", "Gonzalo", "Sergio", "Nicolas", "Leandro", "Emiliano", "Julian", "Lautaro",
             "Cristian", "Rodrigo", "Marcos", "Maximiliano", "Alejandro", "Lisandro", "Enzo", "Alexis", "Nahuel", "German",
             "Exequiel", "Lucas", "Mauro", "Franco", "Guido", "Thiago", "Juan", "Martin", "Pablo", "Diego"],
            ["Martinez", "Gonzalez", "Rodriguez", "Lopez", "Fernandez", "Garcia", "Perez", "Romero", "Diaz", "Gomez",
             "Torres", "Sanchez", "Ramirez", "Flores", "Benitez", "Acosta", "Medina", "Herrera", "Aguero", "Castro",
             "Alvarez", "Ruiz", "Sosa", "Molina", "Cabrera", "Rios", "Morales", "Ortiz", "Gutierrez", "Pereyra"]),

        ["Portugal"] = (
            ["Cristiano", "Bruno", "Bernardo", "Diogo", "Joao", "Ruben", "Goncalo", "Rafael", "Nuno", "Pedro",
             "Andre", "Renato", "Otavio", "Danilo", "Vitinha", "Rui", "Nelson", "Ricardo", "Tiago", "Hugo",
             "Miguel", "Luis", "Antonio", "Francisco", "Manuel", "Paulo", "Carlos", "Jose", "Fernando", "Eduardo"],
            ["Silva", "Santos", "Ferreira", "Pereira", "Oliveira", "Costa", "Rodrigues", "Martins", "Fernandes", "Goncalves",
             "Lopes", "Marques", "Almeida", "Ribeiro", "Pinto", "Carvalho", "Teixeira", "Moreira", "Correia", "Mendes",
             "Nunes", "Sousa", "Vieira", "Monteiro", "Cardoso", "Rocha", "Ramos", "Simoes", "Dias", "Matos"]),

        ["Netherlands"] = (
            ["Virgil", "Frenkie", "Memphis", "Matthijs", "Daley", "Steven", "Denzel", "Wout", "Cody", "Xavi",
             "Jurrien", "Marten", "Teun", "Donyell", "Jeremie", "Nathan", "Ryan", "Jerdy", "Tijjani", "Kenneth",
             "Bas", "Sven", "Wesley", "Robin", "Arjen", "Dirk", "Jan", "Marco", "Mark", "Edwin"],
            ["de Jong", "van Dijk", "de Ligt", "van de Beek", "van Persie", "Bergkamp", "Kluivert", "Seedorf", "Sneijder", "Robben",
             "Depay", "Wijnaldum", "Dumfries", "Gakpo", "Weghorst", "Timber", "Koopmeiners", "Malen", "Simons", "Gravenberch",
             "Bakker", "Visser", "Smit", "Meijer", "Mulder", "Bos", "Vos", "Peters", "Hendriks", "Dekker"]),

        ["Belgium"] = (
            ["Kevin", "Eden", "Romelu", "Thibaut", "Axel", "Yannick", "Dries", "Leander", "Hans", "Timothy",
             "Youri", "Thorgan", "Thomas", "Michy", "Nacer", "Toby", "Jan", "Simon", "Divock", "Adnan",
             "Dennis", "Christian", "Guillaume", "Laurent", "Mousa", "Radja", "Marouane", "Vincent", "Arthur", "Amadou"],
            ["Hazard", "De Bruyne", "Lukaku", "Courtois", "Mertens", "Witsel", "Alderweireld", "Vertonghen", "Mignolet", "Tielemans",
             "Carrasco", "Dendoncker", "Castagne", "Vanaken", "Praet", "Batshuayi", "Benteke", "Januzaj", "Chadli", "Fellaini",
             "Peeters", "Janssens", "Maes", "Jacobs", "Willems", "Claes", "Goossens", "Wouters", "Mertens", "Lambert"]),

        ["Scotland"] = (
            ["Andrew", "Scott", "Callum", "John", "Kieran", "Billy", "Ryan", "Stuart", "Liam", "Grant",
             "Craig", "James", "Lewis", "Ross", "Graeme", "Fraser", "Alan", "David", "Steven", "Gary",
             "Darren", "Kenny", "Robbie", "Greg", "Kevin", "Angus", "Hamish", "Campbell", "Malcolm", "Ian"],
            ["Robertson", "McTominay", "McGinn", "Tierney", "Gilmour", "Adams", "Armstrong", "Christie", "McGregor", "Patterson",
             "Campbell", "Stewart", "Anderson", "MacDonald", "Murray", "Scott", "Thomson", "Reid", "Ross", "Hamilton",
             "Fraser", "Graham", "Gordon", "Duncan", "Cameron", "Burns", "Walker", "Watson", "Morrison", "Wilson"]),

        ["Republic of Ireland"] = (
            ["Shane", "Seamus", "Matt", "James", "Robbie", "Jeff", "John", "Conor", "Darren", "Kevin",
             "Gavin", "Sean", "Michael", "Aiden", "Ciaran", "Declan", "Stephen", "Damien", "Ian", "Alan",
             "Liam", "Patrick", "Colin", "Brian", "Brendan", "Cillian", "Fionn", "Eoin", "Niall", "Darragh"],
            ["Brady", "Coleman", "Doherty", "Duffy", "Long", "Hendrick", "McCarthy", "McClean", "Maguire", "O'Shea",
             "Murphy", "Kelly", "O'Brien", "Byrne", "Ryan", "Walsh", "O'Sullivan", "O'Connor", "Quinn", "Doyle",
             "Lynch", "Murray", "Brennan", "O'Neill", "Gallagher", "Nolan", "Kavanagh", "Power", "Cunningham", "Flanagan"]),

        ["Wales"] = (
            ["Gareth", "Aaron", "Joe", "Ben", "Daniel", "Ethan", "Connor", "Harry", "Chris", "Wayne",
             "Brennan", "Dylan", "Rhys", "Owen", "Kieffer", "David", "Mark", "Hal", "Neco", "Sorba"],
            ["Bale", "Ramsey", "Allen", "Davies", "Williams", "Moore", "Wilson", "Johnson", "Roberts", "Rodon",
             "Thomas", "James", "Edwards", "Phillips", "Morgan", "Evans", "Lewis", "Griffiths", "Price", "Jenkins"]),

        ["Colombia"] = (
            ["James", "Radamel", "David", "Juan", "Luis", "Carlos", "Yerry", "Davinson", "Duvan", "Santiago",
             "Wilmar", "Mateus", "Jefferson", "Camilo", "Fredy", "Cristian", "Edwin", "Andres", "Daniel", "Jhon"],
            ["Rodriguez", "Falcao", "Ospina", "Cuadrado", "Muriel", "Sanchez", "Mina", "Zapata", "Barrios", "Arias",
             "Diaz", "Uribe", "Quintero", "Mojica", "Lerma", "Borja", "Moreno", "Garcia", "Hernandez", "Torres"]),

        ["Mexico"] = (
            ["Guillermo", "Hirving", "Raul", "Andres", "Carlos", "Hector", "Edson", "Jesus", "Diego", "Javier",
             "Jorge", "Miguel", "Roberto", "Cesar", "Alan", "Luis", "Fernando", "Alexis", "Jose", "Victor"],
            ["Ochoa", "Lozano", "Jimenez", "Guardado", "Herrera", "Alvarez", "Vela", "Dos Santos", "Corona", "Moreno",
             "Gutierrez", "Hernandez", "Reyes", "Marquez", "Salcedo", "Araujo", "Gallardo", "Montes", "Rodriguez", "Sanchez"]),

        ["United States"] = (
            ["Christian", "Tyler", "Weston", "Giovanni", "Sergino", "Timothy", "Brenden", "Matt", "Jordan", "Yunus",
             "Josh", "Brendan", "Miles", "Cameron", "Luca", "Ricardo", "Zack", "Ethan", "Folarin", "Malik"],
            ["Pulisic", "Adams", "McKennie", "Reyna", "Dest", "Weah", "Aaronson", "Turner", "Morris", "Musah",
             "Robinson", "Carter", "Mitchell", "Anderson", "Thomas", "Jackson", "White", "Harris", "Martin", "Thompson"]),

        ["Japan"] = (
            ["Takumi", "Takehiro", "Daichi", "Wataru", "Junya", "Kaoru", "Ritsu", "Ao", "Hiroki", "Yuto",
             "Shinji", "Keisuke", "Makoto", "Yuki", "Koji", "Gaku", "Maya", "Shuichi", "Genki", "Daiki"],
            ["Minamino", "Tomiyasu", "Kamada", "Endo", "Ito", "Mitoma", "Doan", "Tanaka", "Sakai", "Nagatomo",
             "Kagawa", "Honda", "Hasebe", "Yoshida", "Kubo", "Nakamura", "Okazaki", "Osako", "Haraguchi", "Inui"]),

        ["Korea Republic"] = (
            ["Son", "Hwang", "Kim", "Lee", "Park", "Cho", "Jung", "Kwon", "Hong", "Kang",
             "Yun", "Han", "Shin", "Seo", "Jang", "Lim", "Oh", "Moon", "Ryu", "Song"],
            ["Heung-min", "Hee-chan", "Min-jae", "Jae-sung", "Ji-sung", "Young-gwon", "In-beom", "Woo-young", "Chang-hoon", "Seung-ho",
             "Dong-won", "Chan-hee", "Jin-su", "Tae-hwan", "Hyun-soo", "Sung-yong", "Gue-sung", "Jun-ho", "Ui-jo", "Moon-hwan"]),

        ["Denmark"] = (
            ["Christian", "Kasper", "Simon", "Andreas", "Pierre-Emile", "Martin", "Thomas", "Jannik", "Joakim", "Mikkel",
             "Rasmus", "Victor", "Mathias", "Jesper", "Daniel", "Henrik", "Peter", "Lars", "Morten", "Nicklas"],
            ["Eriksen", "Schmeichel", "Kjaer", "Christensen", "Hojbjerg", "Braithwaite", "Delaney", "Vestergaard", "Maehle", "Damsgaard",
             "Jensen", "Nielsen", "Hansen", "Andersen", "Pedersen", "Larsen", "Sorensen", "Rasmussen", "Jorgensen", "Petersen"]),

        ["Sweden"] = (
            ["Zlatan", "Emil", "Victor", "Alexander", "Robin", "Sebastian", "Dejan", "Albin", "Kristoffer", "Mattias",
             "Marcus", "Ludwig", "Gustav", "Filip", "Jesper", "Rasmus", "Pontus", "Erik", "Andreas", "Oscar"],
            ["Ibrahimovic", "Forsberg", "Lindelof", "Isak", "Quaison", "Larsson", "Kulusevski", "Ekdal", "Olsson", "Svanberg",
             "Johansson", "Andersson", "Karlsson", "Nilsson", "Eriksson", "Pettersson", "Svensson", "Gustafsson", "Persson", "Jonsson"]),

        ["Norway"] = (
            ["Erling", "Martin", "Sander", "Morten", "Kristoffer", "Stefan", "Alexander", "Joshua", "Mohamed", "Birger",
             "Jonas", "Fredrik", "Ola", "Thomas", "Markus", "Mathias", "Vegard", "Ole", "Lars", "Jens"],
            ["Haaland", "Odegaard", "Berge", "Thorsby", "Ajer", "Sorloth", "King", "Elyounoussi", "Meling", "Normann",
             "Hansen", "Johansen", "Olsen", "Larsen", "Andersen", "Pedersen", "Nilsen", "Kristiansen", "Jensen", "Karlsen"]),

        ["Poland"] = (
            ["Robert", "Wojciech", "Arkadiusz", "Piotr", "Grzegorz", "Kamil", "Jan", "Mateusz", "Jakub", "Bartosz",
             "Krzysztof", "Przemyslaw", "Dawid", "Sebastian", "Maciej", "Lukasz", "Pawel", "Damian", "Szymon", "Tomasz"],
            ["Lewandowski", "Szczesny", "Milik", "Zielinski", "Krychowiak", "Glik", "Bednarek", "Klich", "Moder", "Linetty",
             "Kowalski", "Wisniewski", "Wojcik", "Kaminski", "Lewandowski", "Zielinski", "Szymanski", "Wozniak", "Dabrowski", "Kozlowski"]),

        ["Turkey"] = (
            ["Hakan", "Cengiz", "Burak", "Ozan", "Merih", "Kenan", "Irfan", "Yusuf", "Enes", "Caglar",
             "Emre", "Arda", "Cenk", "Umut", "Oguzhan", "Selcuk", "Gokhan", "Mehmet", "Mert", "Serdar"],
            ["Calhanoglu", "Under", "Yilmaz", "Kabak", "Demiral", "Karaman", "Kahveci", "Yazici", "Unal", "Soyuncu",
             "Yildirim", "Kaya", "Demir", "Celik", "Sahin", "Ozturk", "Arslan", "Dogan", "Kilic", "Polat"]),

        ["Russia"] = (
            ["Artem", "Denis", "Aleksey", "Fedor", "Daler", "Anton", "Andrey", "Yuri", "Igor", "Dmitri",
             "Aleksandr", "Stanislav", "Mario", "Roman", "Vyacheslav", "Kirill", "Maxim", "Sergei", "Mikhail", "Vladimir"],
            ["Dzyuba", "Cheryshev", "Miranchuk", "Smolov", "Kuzyaev", "Shunin", "Golovin", "Zhirkov", "Fernandes", "Mostovoy",
             "Ivanov", "Smirnov", "Kuznetsov", "Popov", "Sokolov", "Lebedev", "Kozlov", "Novikov", "Morozov", "Petrov"]),

        ["Croatia"] = (
            ["Luka", "Ivan", "Mateo", "Marcelo", "Ante", "Andrej", "Josip", "Dejan", "Borna", "Josko",
             "Nikola", "Mislav", "Duje", "Mario", "Domagoj", "Marko", "Bruno", "Sime", "Lovro", "Martin"],
            ["Modric", "Perisic", "Kovacic", "Brozovic", "Rebic", "Kramaric", "Rakitic", "Mandzukic", "Lovren", "Vida",
             "Horvat", "Kovac", "Babic", "Maric", "Juric", "Novak", "Knezevic", "Vukovic", "Matic", "Blazevic"]),

        ["Switzerland"] = (
            ["Xherdan", "Granit", "Haris", "Yann", "Breel", "Ricardo", "Djibril", "Remo", "Denis", "Nico",
             "Fabian", "Renato", "Admir", "Steven", "Kevin", "Manuel", "Ruben", "Silvan", "Christian", "Stephan"],
            ["Shaqiri", "Xhaka", "Seferovic", "Sommer", "Embolo", "Rodriguez", "Sow", "Freuler", "Zakaria", "Elvedi",
             "Muller", "Steffen", "Mehmedi", "Zuber", "Mbabu", "Akanji", "Vargas", "Widmer", "Frei", "Gavranovic"]),

        ["Austria"] = (
            ["David", "Marko", "Marcel", "Konrad", "Christoph", "Valentino", "Stefan", "Xaver", "Alessandro", "Michael",
             "Florian", "Andreas", "Patrick", "Martin", "Julian", "Philipp", "Maximilian", "Kevin", "Daniel", "Peter"],
            ["Alaba", "Arnautovic", "Sabitzer", "Laimer", "Baumgartner", "Hinteregger", "Dragovic", "Lainer", "Grillitsch", "Schlager",
             "Huber", "Wagner", "Bauer", "Berger", "Pichler", "Steiner", "Moser", "Mayer", "Hofer", "Leitner"]),

        ["Australia"] = (
            ["Mat", "Aaron", "Bailey", "Ajdin", "Jackson", "Mathew", "Jamie", "Aziz", "Thomas", "Riley",
             "Mitchell", "Andrew", "Craig", "Daniel", "Trent", "Rhyan", "Harry", "Kye", "Jason", "Martin"],
            ["Ryan", "Mooy", "Wright", "Hrustic", "Irvine", "Leckie", "MacLaren", "Behich", "Souttar", "Rowles",
             "Williams", "Taylor", "Brown", "Wilson", "Anderson", "Thompson", "Martin", "Smith", "Walker", "Robinson"]),

        ["Nigeria"] = (
            ["Victor", "Kelechi", "Wilfred", "Ahmed", "Alex", "Samuel", "William", "Moses", "Oghenekaro", "Ola",
             "Emmanuel", "Kenneth", "Umar", "Odion", "Leon", "Calvin", "Isaac", "Frank", "Henry", "Joseph"],
            ["Osimhen", "Iheanacho", "Ndidi", "Musa", "Iwobi", "Chukwueze", "Troost-Ekong", "Simon", "Aina", "Aribo",
             "Okoye", "Bassey", "Onyeka", "Lookman", "Awoniyi", "Dennis", "Ighalo", "Ejuke", "Balogun", "Uzoho"]),

        ["Ghana"] = (
            ["Thomas", "Mohammed", "Andre", "Daniel", "Jordan", "Inaki", "Tariq", "Kamaldeen", "Antoine", "Joseph",
             "Abdul", "Jeffrey", "Alexander", "Baba", "Harrison", "Elisha", "Lawrence", "Felix", "Osman", "Ibrahim"],
            ["Partey", "Kudus", "Ayew", "Amartey", "Schlupp", "Sulemana", "Semenyo", "Williams", "Lamptey", "Afena-Gyan",
             "Rahman", "Mensah", "Owusu", "Agyemang", "Boateng", "Appiah", "Asante", "Opoku", "Yeboah", "Asamoah"]),

        ["Ivory Coast"] = (
            ["Nicolas", "Wilfried", "Franck", "Serge", "Eric", "Simon", "Maxwel", "Sebastien", "Ibrahim", "Jean",
             "Gervinho", "Cheick", "Yaya", "Kolo", "Salomon", "Didier", "Arouna", "Lacina", "Boubacar", "Souleymane"],
            ["Pepe", "Zaha", "Kessie", "Aurier", "Bailly", "Deli", "Cornet", "Haller", "Sangare", "Boly",
             "Toure", "Gradel", "Seri", "Gbamin", "Diallo", "Konan", "Traore", "Coulibaly", "Drogba", "Kalou"]),

        ["Senegal"] = (
            ["Sadio", "Kalidou", "Edouard", "Ismaila", "Idrissa", "Boulaye", "Abdou", "Cheikhou", "Pape", "Krepin",
             "Moussa", "Famara", "Saliou", "Habib", "Keita", "Nampalys", "Alfred", "Lamine", "Bouna", "Bamba"],
            ["Mane", "Koulibaly", "Mendy", "Sarr", "Gueye", "Dia", "Diallo", "Kouyate", "Ciss", "Diatta",
             "Ndiaye", "Diedhiou", "Cisse", "Diagne", "Niasse", "Sow", "Ba", "Fall", "Gassama", "Diouf"]),

        ["Cameroon"] = (
            ["Andre", "Eric", "Vincent", "Karl", "Jean-Pierre", "Martin", "Samuel", "Christian", "Moumi", "Clinton",
             "Pierre", "Olivier", "Nicolas", "Collins", "Bryan", "Gaetan", "Frank", "Michael", "Jean", "Stephane"],
            ["Onana", "Choupo-Moting", "Aboubakar", "Toko Ekambi", "Anguissa", "Njie", "Hongla", "Fai", "Ngamaleu", "Mbeumo",
             "Nkoulou", "Bassogog", "Ondoa", "Moukandjo", "Siani", "Oyongo", "Marou", "Zoua", "Tchato", "Moukoudi"]),

        ["Uruguay"] = (
            ["Luis", "Edinson", "Federico", "Diego", "Jose", "Ronald", "Matias", "Rodrigo", "Martin", "Nahitan",
             "Facundo", "Giorgian", "Agustin", "Maximiliano", "Mauro", "Sebastian", "Gaston", "Nicolas", "Mathias", "Darwin"],
            ["Suarez", "Cavani", "Valverde", "Godin", "Gimenez", "Araujo", "Vecino", "Bentancur", "Caceres", "Nandez",
             "Torres", "De Arrascaeta", "Pellistri", "Nunez", "Olivera", "Canobbio", "Vina", "Coates", "Muslera", "Torreira"]),

        ["Chile"] = (
            ["Alexis", "Arturo", "Gary", "Charles", "Eduardo", "Claudio", "Erick", "Mauricio", "Jose", "Eugenio",
             "Guillermo", "Nicolas", "Sebastian", "Fabian", "Pablo", "Angelo", "Marcelo", "Francisco", "Diego", "Benjamin"],
            ["Sanchez", "Vidal", "Medel", "Aranguiz", "Vargas", "Bravo", "Pulgar", "Isla", "Diaz", "Mena",
             "Sierralta", "Maripan", "Nunez", "Pavez", "Fuenzalida", "Gutierrez", "Mora", "Henriquez", "Reyes", "Perez"]),

        ["Saudi Arabia"] = (
            ["Mohammed", "Salem", "Saleh", "Fahad", "Abdullah", "Sami", "Yasser", "Firas", "Nawaf", "Ali",
             "Hassan", "Hattan", "Turki", "Taisir", "Abdulelah", "Saud", "Abdulrahman", "Nasser", "Khalid", "Ahmed"],
            ["Al-Shehri", "Al-Dawsari", "Al-Owais", "Al-Buraik", "Al-Shahrani", "Al-Faraj", "Al-Muwallad", "Al-Amri", "Al-Tambakti", "Al-Hassan",
             "Al-Malki", "Al-Ghannam", "Al-Qahtani", "Al-Sahlawi", "Al-Jassam", "Al-Mousa", "Al-Harbi", "Al-Otaibi", "Al-Zahrani", "Al-Dosari"]),

        ["Czech Republic"] = (
            ["Tomas", "Patrik", "Vladimir", "Jakub", "Adam", "Jan", "Ondrej", "Pavel", "Antonin", "Lukas",
             "David", "Ales", "Filip", "Martin", "Petr", "Daniel", "Michal", "Vaclav", "Matej", "Jiri"],
            ["Soucek", "Schick", "Coufal", "Jankto", "Darida", "Kral", "Holes", "Masopust", "Kalas", "Celustka",
             "Novak", "Svoboda", "Novotny", "Dvorak", "Hajek", "Pokorny", "Vesely", "Horak", "Nemec", "Marek"]),

        ["Romania"] = (
            ["Nicolae", "Razvan", "Florin", "Vlad", "Denis", "Ionut", "Marius", "George", "Ciprian", "Alexandru",
             "Andrei", "Cristian", "Adrian", "Ianis", "Valentin", "Constantin", "Stefan", "Mihai", "Dragos", "Cosmin"],
            ["Stanciu", "Marin", "Hagi", "Chiriches", "Mitrita", "Tatarusanu", "Keseru", "Maxim", "Bancu", "Nedelcu",
             "Popa", "Ionescu", "Dumitru", "Stan", "Moldovan", "Lupescu", "Petrescu", "Radulescu", "Stoica", "Georgescu"]),

        ["Serbia"] = (
            ["Dusan", "Aleksandar", "Sergej", "Nikola", "Filip", "Nemanja", "Luka", "Strahinja", "Sasa", "Andrija",
             "Mijat", "Ivan", "Marko", "Stefan", "Darko", "Branislav", "Dejan", "Adem", "Uros", "Vladimir"],
            ["Vlahovic", "Mitrovic", "Milinkovic-Savic", "Milenkovic", "Kostic", "Gudelj", "Jovic", "Pavlovic", "Lukic", "Zivkovic",
             "Jovanovic", "Petrovic", "Nikolic", "Djordjevic", "Ivanovic", "Stankovic", "Vidic", "Matic", "Tadic", "Maksimovic"]),

        ["Greece"] = (
            ["Giorgos", "Sokratis", "Kostas", "Dimitrios", "Anastasios", "Panagiotis", "Vangelis", "Manolis", "Efthymios", "Petros",
             "Konstantinos", "Thanasis", "Evangelos", "Christos", "Alexandros", "Dimitris", "Nikos", "Ioannis", "Spyros", "Vassilis"],
            ["Tzavellas", "Papastathopoulos", "Manolas", "Fortounis", "Bakasetas", "Retsos", "Pavlidis", "Limnios", "Siopis", "Tsimikas",
             "Papadopoulos", "Georgiou", "Nikolaou", "Karagounis", "Samaras", "Katsouranis", "Torosidis", "Salpingidis", "Mitroglou", "Holebas"]),

        ["Ukraine"] = (
            ["Andriy", "Oleksandr", "Mykola", "Viktor", "Ruslan", "Roman", "Taras", "Serhiy", "Vitaliy", "Denys",
             "Artem", "Eduard", "Bogdan", "Illia", "Yevhen", "Dmytro", "Anatoliy", "Vladyslav", "Maksym", "Oleksiy"],
            ["Shevchenko", "Zinchenko", "Mykolenko", "Tsygankov", "Malinovskyi", "Malinovsky", "Yarmolenko", "Mudryk", "Stepanenko", "Sydorchuk",
             "Kovalenko", "Bondar", "Zabarnyi", "Shaparenko", "Dovbyk", "Lunin", "Trubin", "Matvienko", "Tymchyk", "Zubkov"]),

        ["Morocco"] = (
            ["Achraf", "Hakim", "Youssef", "Sofyan", "Azzedine", "Nayef", "Romain", "Selim", "Sofiane", "Yassine",
             "Jawad", "Munir", "Nabil", "Nordin", "Adel", "Karim", "Mehdi", "Amine", "Omar", "Hamza"],
            ["Hakimi", "Ziyech", "En-Nesyri", "Amrabat", "Ounahi", "Aguerd", "Saiss", "Amallah", "Boufal", "Bounou",
             "El Kaabi", "Mazraoui", "Belhanda", "Dirar", "Boussoufa", "Benatia", "El Ahmadi", "Feddal", "Tannane", "Cheddira"]),

        ["Algeria"] = (
            ["Riyad", "Islam", "Said", "Ismael", "Yacine", "Aissa", "Ramy", "Djamel", "Andy", "Baghdad",
             "Haris", "Adlene", "Adam", "Youcef", "Mehdi", "Hillel", "Abdelkader", "Sofiane", "Farid", "Nabil"],
            ["Mahrez", "Slimani", "Bennacer", "Benrahma", "Brahimi", "Mandi", "Bensebaini", "Benlamri", "Delort", "Bounedjah",
             "Feghouli", "Ounas", "Atal", "Guedioura", "Belkebla", "Zorgane", "Zerrouki", "Boulaya", "Tahrat", "Belaili"]),

        ["Tunisia"] = (
            ["Youssef", "Wahbi", "Ellyes", "Aissa", "Ali", "Hannibal", "Mohamed", "Issam", "Naim", "Hamza",
             "Ferjani", "Dylan", "Montassar", "Ghaylen", "Seifeddine", "Bilel", "Farouk", "Anis", "Wajdi", "Rami"],
            ["Msakni", "Khazri", "Skhiri", "Laidouni", "Maaloul", "Mejbri", "Talbi", "Jebali", "Slimane", "Mathlouthi",
             "Ben Youssef", "Haddadi", "Meriah", "Sassi", "Bronn", "Chaalali", "Jaziri", "Khenissi", "Drager", "Bedoui"]),

        ["China PR"] = (
            ["Wu", "Zhang", "Li", "Wang", "Hao", "Wei", "Lei", "Yan", "Zheng", "Lin",
             "Gao", "Liu", "Chen", "Yang", "Xu", "Sun", "Ma", "Zhao", "Feng", "Jiang"],
            ["Lei", "Linpeng", "Xiang", "Zhiyuan", "Junmin", "Haidong", "Dalei", "Zhi", "Ke", "Xi",
             "Jun", "Hai", "Shihao", "Liangming", "Binbin", "Song", "Cheng", "Xiaoting", "Xu", "Ang"]),

        ["India"] = (
            ["Sunil", "Gurpreet", "Sandesh", "Ashique", "Brandon", "Anirudh", "Sahal", "Lallianzuala", "Udanta", "Liston",
             "Rahul", "Manvir", "Pritam", "Jeakson", "Ishan", "Akash", "Prabhsukhan", "Dheeraj", "Anwar", "Vikram"],
            ["Chhetri", "Singh", "Jhingan", "Kuruniyan", "Fernandes", "Thapa", "Kattimani", "Chhangte", "Singh", "Colaco",
             "Bheke", "Kumar", "Kotal", "Thounaojam", "Pandita", "Mishra", "Gill", "Moirangthem", "Ali", "Pratap"]),

        ["Paraguay"] = (
            ["Miguel", "Gustavo", "Oscar", "Derlis", "Robert", "Fabian", "Antonio", "Angel", "Hernan", "Junior",
             "Santiago", "Julio", "Rodrigo", "Blas", "Mathias", "Alberto", "Ivan", "Alan", "Nelson", "Carlos"],
            ["Almiron", "Gomez", "Cardozo", "Gonzalez", "Piris", "Balbuena", "Sanabria", "Romero", "Perez", "Alonso",
             "Villasanti", "Arzamendia", "Caceres", "Riveros", "Dominguez", "Escobar", "Gamarra", "Santa Cruz", "Valdez", "Haedo"]),

        ["Ecuador"] = (
            ["Enner", "Antonio", "Moises", "Angel", "Pervis", "Gonzalo", "Alan", "Romario", "Jeremy", "Jhegson",
             "Michael", "Jackson", "Carlos", "Felix", "Joao", "Byron", "Piero", "Ayrton", "Diego", "Jordan"],
            ["Valencia", "Caicedo", "Estupinan", "Mena", "Plata", "Ibarra", "Franco", "Hincapie", "Sarmiento", "Mendez",
             "Estrada", "Arboleda", "Gruezo", "Torres", "Ramirez", "Cifuentes", "Castillo", "Preciado", "Reasco", "Paez"]),

        ["Peru"] = (
            ["Paolo", "Edison", "Christian", "Andre", "Pedro", "Marcos", "Renato", "Yoshimar", "Luis", "Alexander",
             "Jefferson", "Carlos", "Anderson", "Miguel", "Gianluca", "Sergio", "Raul", "Andy", "Bryan", "Santiago"],
            ["Guerrero", "Flores", "Cueva", "Carrillo", "Aquino", "Lopez", "Tapia", "Yotun", "Advincula", "Callens",
             "Farfan", "Trauco", "Zambrano", "Abram", "Lapadula", "Pena", "Ruidiaz", "Polo", "Gonzales", "Ormeño"]),

        ["Venezuela"] = (
            ["Salomon", "Yeferson", "Yangel", "Josef", "Darwin", "Jhon", "Tomas", "Eric", "Oscar", "Roberto",
             "Rolf", "Edson", "Mikel", "Jan", "Jefferson", "Fernando", "Cristian", "Jose", "Ronald", "Sergio"],
            ["Rondon", "Soteldo", "Herrera", "Martinez", "Machis", "Chancellor", "Rincon", "Ramirez", "Gonzalez", "Rosales",
             "Villanueva", "Osorio", "Savarino", "Otero", "Cordova", "Cichero", "Arango", "Vizcarrondo", "Miku", "Penaranda"]),

        ["Bolivia"] = (
            ["Marcelo", "Ramiro", "Carlos", "Erwin", "Leonel", "Jeyson", "Boris", "Henry", "Diego", "Fernando",
             "Samuel", "Adrian", "Gabriel", "Luis", "Rodrigo", "Alejandro", "Jose", "Victor", "Moises", "Jaime"],
            ["Martins", "Vaca", "Lampe", "Saucedo", "Justiniano", "Chura", "Cespedes", "Vaca", "Bejarano", "Arce",
             "Moreno", "Fernandez", "Alvarez", "Gutierrez", "Haquin", "Sagredo", "Galindo", "Ribera", "Villarroel", "Flores"]),

        ["Costa Rica"] = (
            ["Keylor", "Bryan", "Joel", "Oscar", "Celso", "Ronald", "Yeltsin", "Kendall", "Jewison", "Gerson",
             "Anthony", "Daniel", "Francisco", "Johan", "Youstin", "Brandon", "Keysher", "Alvaro", "Ian", "Jimmy"],
            ["Navas", "Ruiz", "Campbell", "Duarte", "Borges", "Matarrita", "Tejeda", "Waston", "Bennette", "Torres",
             "Contreras", "Calvo", "Gamboa", "Gonzalez", "Vargas", "Hernandez", "Mora", "Leal", "Oviedo", "Salas"]),

        ["Jamaica"] = (
            ["Leon", "Michail", "Bobby", "Kemar", "Damion", "Shamar", "Ravel", "Andre", "Oniel", "Ethan",
             "Joel", "Lamar", "Adrian", "Dillon", "Greg", "Javain", "Ricardo", "Kevon", "Alvas", "Devon"],
            ["Bailey", "Antonio", "De Cordova-Reid", "Roofe", "Lowe", "Nicholson", "Morrison", "Gray", "Fisher", "Pinnock",
             "Blake", "Lawrence", "Mariappa", "Powell", "Williams", "Morgan", "Thomas", "Brown", "Johnson", "Campbell"]),

        ["Honduras"] = (
            ["Alberth", "Romell", "Andy", "Maynor", "Emilio", "Deybi", "Luis", "Kevin", "Diego", "Kervin",
             "Bryan", "Jonathan", "Edwin", "Carlos", "Jose", "Walter", "Oscar", "Henry", "Ever", "Rigoberto"],
            ["Elis", "Quioto", "Najar", "Figueroa", "Izaguirre", "Flores", "Lopez", "Alvarez", "Rodriguez", "Arriaga",
             "Espinoza", "Costly", "Beckeles", "Palacios", "Garrido", "Garcia", "Hernandez", "Chavez", "Maldonado", "Mejia"]),

        ["Northern Ireland"] = (
            ["Steven", "Jonny", "Stuart", "Paddy", "Conor", "Craig", "Niall", "Liam", "Jamal", "Jordan",
             "Josh", "Gavin", "Shane", "Kyle", "Daniel", "Bailey", "Ali", "Dion", "Shea", "Ciaron"],
            ["Davis", "Evans", "Dallas", "McNair", "Washington", "Cathcart", "McGinn", "Boyce", "Lafferty", "Saville",
             "Thompson", "Ferguson", "Magennis", "Peacock-Farrell", "Bradley", "Flanagan", "Whyte", "McCann", "Lewis", "Donnelly"]),

        ["Finland"] = (
            ["Teemu", "Robin", "Joel", "Rasmus", "Glen", "Lukas", "Daniel", "Fredrik", "Jere", "Joona",
             "Nikolai", "Pyry", "Joni", "Marcus", "Paulus", "Lassi", "Tim", "Leo", "Ilmari", "Robert"],
            ["Pukki", "Lod", "Pohjanpalo", "Schuller", "Kamara", "Hradecky", "O'Shaughnessy", "Jensen", "Uronen", "Toivio",
             "Virtanen", "Makinen", "Nieminen", "Koskinen", "Jarvinen", "Korhonen", "Lehtinen", "Heikkinen", "Maki", "Hakkinen"]),

        ["Iceland"] = (
            ["Gylfi", "Aron", "Albert", "Birkir", "Johann", "Kolbeinn", "Ragnar", "Kari", "Holmar", "Victor",
             "Runar", "Sverrir", "Jon", "Arnor", "Mikael", "Brynjar", "Hjortur", "Vidar", "Ari", "Alfred"],
            ["Sigurdsson", "Gunnarsson", "Gudmundsson", "Bjarnason", "Sigthorsson", "Sigthórsson", "Halldorsson", "Arnason", "Hermannsson", "Palsson",
             "Magnusson", "Traustason", "Ingason", "Saevarsson", "Skulason", "Finnbogason", "Bodvarsson", "Eyjolfsson", "Fridjonsson", "Thorsteinsson"]),

        ["Hungary"] = (
            ["Dominik", "Willi", "Attila", "Roland", "Adam", "Zsolt", "Loic", "Andras", "Kevin", "Daniel",
             "Peter", "Balazs", "Gergo", "Tamas", "Mihaly", "Barnabas", "Callum", "David", "Norbert", "Bence"],
            ["Szoboszlai", "Orban", "Szalai", "Sallai", "Nagy", "Nego", "Fiola", "Schafer", "Styles", "Lang",
             "Kovacs", "Horvath", "Szabo", "Toth", "Varga", "Molnar", "Nemeth", "Farkas", "Balogh", "Kiss"])
    };

    internal static readonly string[] Positions = ["GK", "CB", "LB", "RB", "CDM", "CM", "CAM", "LM", "RM", "LW", "RW", "ST"];

    internal static readonly string[] WorkRates = ["High / High", "High / Medium", "High / Low", "Medium / High", "Medium / Medium", "Medium / Low", "Low / Medium", "Low / Low"];
}
