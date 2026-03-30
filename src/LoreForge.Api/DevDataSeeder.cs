using LoreForge.Core.Entities;
using LoreForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class DevDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LoreForgeDbContext>();

        if (await db.Works.AnyAsync()) return;

        var zeroVector = new float[1024];

        var works = new List<Work>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Elden Ring",
                Type = WorkType.Game,
                Genres = ["action", "rpg", "souls-like"],
                Status = WorkStatus.Completed,
                Tags = ["fromsoft", "open-world", "dark-fantasy"],
                Notes = new WorkNotes
                {
                    Worldbuilding = "The Lands Between — a shattered realm ruled by demigods fighting over shards of the Elden Ring.",
                    Magic = "The Elden Ring shattered into Great Runes, each held by a demigod. Grace guides the Tarnished.",
                    Characters = "Melina, Ranni, Marika, Godrick, Radahn, Malenia.",
                    Themes = "Death, renewal, cycles of power, the burden of ambition.",
                    PlotStructure = "Open world boss rush — player-driven pacing with optional legacy dungeons.",
                    WhatILiked = "Incredible environmental storytelling. Every corner hides a detail. Malenia is peak boss design."
                },
                Embedding = zeroVector
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "The Name of the Wind",
                Type = WorkType.Book,
                Genres = ["fantasy", "magic", "coming-of-age"],
                Status = WorkStatus.Completed,
                Tags = ["rothfuss", "kingkiller-chronicle"],
                Notes = new WorkNotes
                {
                    Worldbuilding = "The Four Corners of Civilization. Magic is called Sympathy — it has rules, costs, and limits.",
                    Magic = "Sympathy: binding two objects so energy flows between them. Naming: knowing the true name of things grants control over them.",
                    Characters = "Kvothe (unreliable narrator), Denna, Bast, Chronicler.",
                    Themes = "Legend vs. reality, the cost of genius, storytelling as identity.",
                    WhatILiked = "Best prose I've read in fantasy. Kvothe's tragedy is that he learns everything except how to lead without burning bridges."
                },
                Embedding = zeroVector
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Arcane",
                Type = WorkType.Series,
                Genres = ["animation", "sci-fi", "tragedy"],
                Status = WorkStatus.Completed,
                Tags = ["league-of-legends", "netflix", "steampunk"],
                Notes = new WorkNotes
                {
                    Worldbuilding = "Piltover (wealthy, progressive) vs. Zaun (industrial undercity). Magic through Hextech — crystals infused with arcane power.",
                    Characters = "Vi, Jinx/Powder, Jayce, Viktor, Silco, Caitlyn.",
                    Themes = "Class war, the corruption of progress, broken sisterhood, what we sacrifice for power.",
                    PlotStructure = "Three-act across two seasons. Each episode ends on a gut punch.",
                    WhatILiked = "The animation is breathtaking. Silco is one of the best antagonists ever written — you understand him completely."
                },
                Embedding = zeroVector
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Dune",
                Type = WorkType.Book,
                Genres = ["sci-fi", "epic", "political"],
                Status = WorkStatus.Completed,
                Tags = ["herbert", "messiah", "ecology"],
                Notes = new WorkNotes
                {
                    Worldbuilding = "Arrakis — only source of the Spice Melange, which enables interstellar travel. Fremen culture built around water scarcity.",
                    Magic = "The Bene Gesserit Weirding Way, prescience from Spice, the Voice.",
                    Characters = "Paul Atreides, Lady Jessica, Duncan Idaho, Stilgar, Chani, Leto II.",
                    Themes = "Ecological collapse, the danger of messianic thinking, colonialism, religious manipulation.",
                    WhatILiked = "Herbert deliberately wrote Paul as a warning, not a hero. Most readers miss it on a first read."
                },
                Embedding = zeroVector
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Disco Elysium",
                Type = WorkType.Game,
                Genres = ["rpg", "mystery", "political"],
                Status = WorkStatus.Completed,
                Tags = ["zaum", "narrative", "detective"],
                Notes = new WorkNotes
                {
                    Worldbuilding = "Revachol — a failed revolutionary city under colonial occupation. The world runs on Pale, a metaphysical entropy that erases reality.",
                    Characters = "Harry Du Bois, Kim Kitsuragi, Jean Vicquemare, Cuno.",
                    Themes = "Political disillusionment, memory and identity, the ruins of ideology, addiction as self-erasure.",
                    WhatILiked = "The skill system IS the narrative. Your internalized voices argue with each other. Nothing else like it."
                },
                Embedding = zeroVector
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Blade Runner 2049",
                Type = WorkType.Movie,
                Genres = ["sci-fi", "neo-noir", "dystopia"],
                Status = WorkStatus.Completed,
                Tags = ["villeneuve", "cyberpunk", "sequel"],
                Notes = new WorkNotes
                {
                    Worldbuilding = "Collapsed ecology. Wallace Corporation replaced Tyrell. Replicants may now reproduce.",
                    Characters = "K/Joe, Joi, Ana Stelline, Niander Wallace, Deckard.",
                    Themes = "What makes a memory real, the construction of identity, exploitation of the Other.",
                    WhatILiked = "Every frame is a painting. The Joi relationship is quietly devastating — love between two manufactured beings."
                },
                Embedding = zeroVector
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "The Last of Us",
                Type = WorkType.Series,
                Genres = ["drama", "post-apocalyptic", "horror"],
                Status = WorkStatus.InProgress,
                Tags = ["naughty-dog", "hbo", "adaptation"],
                Notes = new WorkNotes
                {
                    Characters = "Joel, Ellie, Tess, Bill, Frank, Kathleen.",
                    Themes = "Grief as armor, the cost of love, what we protect and what we destroy to do it.",
                    WhatILiked = "Episode 3 (Bill and Frank) is one of the best hours of television ever made."
                },
                Embedding = zeroVector
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Shadow of the Colossus",
                Type = WorkType.Game,
                Genres = ["action", "puzzle", "art-game"],
                Status = WorkStatus.Completed,
                Tags = ["ico", "team-ico", "minimalist"],
                Notes = new WorkNotes
                {
                    Worldbuilding = "A forbidden land. Sixteen colossi. One deal with a dark deity.",
                    Themes = "The cost of obsession, love as destruction, whether the player is the villain.",
                    WhatILiked = "Every colossus you kill feels wrong. The game never lets you forget what you're doing."
                },
                Embedding = zeroVector
            }
        };

        db.Works.AddRange(works);
        await db.SaveChangesAsync();
    }
}
