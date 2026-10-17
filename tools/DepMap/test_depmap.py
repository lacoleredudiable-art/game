import re
import tempfile
import unittest
from pathlib import Path

import depmap

OLD = [re.compile(r"(^|/)Old[^/]*\.cs$")]


def make(files):
    d = Path(tempfile.mkdtemp())
    for rel, text in files.items():
        p = d / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text, encoding="utf-8")
    return d


class DepMapTests(unittest.TestCase):
    def test_transitive_reach_counts_old_file(self):
        d = make({
            "RuleEngineV4/Root.cs": "class Root { Bridge b; }",
            "Game/Bridge.cs": "class Bridge { OldDirector d; }",
            "Game/OldDirector.cs": "class OldDirector { }",
        })
        self.assertEqual(depmap.analyze(d, OLD)["old"], ["Game/OldDirector.cs"])

    def test_unreachable_old_file_not_counted(self):
        d = make({
            "RuleEngineV4/Root.cs": "class Root { }",
            "Game/OldDirector.cs": "class OldDirector { }",
        })
        r = depmap.analyze(d, OLD)
        self.assertEqual(r["old"], [])
        self.assertEqual(r["old_all"], ["Game/OldDirector.cs"])

    def test_partial_class_files_all_reached(self):
        d = make({
            "RuleEngineV4/Root.cs": "class Root { OldDirector d; }",
            "Game/OldDirector.cs": "partial class OldDirector { }",
            "Game/OldDirector.Part.cs": "partial class OldDirector { }",
        })
        self.assertEqual(len(depmap.analyze(d, OLD)["old"]), 2)

    def test_comment_and_string_mentions_ignored(self):
        d = make({
            "RuleEngineV4/Root.cs": '// OldDirector\nclass Root { string s = "OldDirector"; }',
            "Game/OldDirector.cs": "class OldDirector { }",
        })
        self.assertEqual(depmap.analyze(d, OLD)["old"], [])

    def test_v4_root_file_never_counted_as_old(self):
        d = make({"RuleEngineV4/OldRoot.cs": "class OldRoot { }"})
        self.assertEqual(depmap.analyze(d, OLD)["old"], [])

    def test_real_pattern_file_loads(self):
        self.assertTrue(depmap.load_patterns(depmap.DEFAULT_LIST))


if __name__ == "__main__":
    unittest.main()
